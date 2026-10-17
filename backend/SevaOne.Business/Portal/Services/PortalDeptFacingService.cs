using SevaOne.Business.Portal.DTOs;
using SevaOne.Business.Portal.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;
using SevaOne.Business.Common;
using SevaOne.Domain.Common;
using SevaOne.Domain.Enums;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Discovery;
using SevaOne.Infrastructure.Repositories.Orchestration;
using SevaOne.Infrastructure.Repositories.Portal;
using SevaOne.Infrastructure.Repositories.Status;
using SevaOne.Infrastructure.Security;

namespace SevaOne.Business.Portal.Services;

public sealed class PortalDeptFacingService : IPortalDeptFacingService
{
    private readonly IDeptTokenGate _gate;
    private readonly IPortalRepository _portal;
    private readonly IDiscoveryRepository _discovery;
    private readonly IOrchestrationRepository _orchestration;
    private readonly IStatusRepository _status;
    private readonly IJwsTokenService _jws;
    private readonly IApplyRedirectProtector _redirectProtector;
    private readonly IClock _clock;
    private readonly IMapper _mapper;
    private readonly ILogger<PortalDeptFacingService> _log;

    public PortalDeptFacingService(
        IDeptTokenGate gate,
        IPortalRepository portal,
        IDiscoveryRepository discovery,
        IOrchestrationRepository orchestration,
        IStatusRepository status,
        IJwsTokenService jws,
        IApplyRedirectProtector redirectProtector,
        IClock clock,
        IMapper mapper,
        ILogger<PortalDeptFacingService> log)
    {
        _gate = gate;
        _portal = portal;
        _discovery = discovery;
        _orchestration = orchestration;
        _status = status;
        _jws = jws;
        _redirectProtector = redirectProtector;
        _clock = clock;
        _mapper = mapper;
        _log = log;
    }

    /// <summary>
    /// User Details API: the single redeem call for both "already know this citizen" and
    /// "first time seeing this citizen" -- formerly two endpoints (Token Validate / Get User
    /// Details) with two different response shapes; a department no longer has to look anything
    /// up locally before deciding which one to call. One-time redeem either way.
    /// </summary>
    public async Task<UserDetailsResult> GetUserDetailsAsync(GetUserDetailsRequest request, CancellationToken ct)
    {
        var resolution = await _gate.ResolveAsync(request.DepartmentId, request.Token, ct);
        if (!resolution.Success || resolution.Token is not { } token)
        {
            TokenJourneyLog.Emit(_log, "T6", "REJECT", "Get User Details — token invalid", ("reason", resolution.FailureReason));
            throw new ForbiddenException("Token is invalid or does not belong to this department.", SevaErrorCodes.Dept.TokenInvalid);
        }

        if (_clock.UtcNow > token.ExpireAt)
        {
            TokenJourneyLog.Emit(_log, "T6", "REJECT", "Get User Details — expired", ("sevaOneUserId", token.SevaOneUserId.ToString()));
            throw new ForbiddenException("Token has expired. The 30-minute session has ended.", SevaErrorCodes.Dept.TokenExpired);
        }

        if (!await _gate.TryRedeemAsync(token, ct))
        {
            TokenJourneyLog.Emit(_log, "T6", "REJECT", "Get User Details — replay", ("sevaOneUserId", token.SevaOneUserId.ToString()));
            throw new ForbiddenException("Token has already been used.", SevaErrorCodes.Dept.TokenAlreadyUsed);
        }

        var scheme = await _discovery.GetSchemeByIdAsync(token.SchemeId, ct)
                     ?? throw new NotFoundException("Scheme not found.", SevaErrorCodes.Dept.SchemeNotFound);
        string? operatorCode = null;
        if (token.OperatorId is { } operatorId)
        {
            operatorCode = (await _portal.GetOperatorByIdAsync(operatorId, ct))?.OperatorCode;
        }

        var citizen = await _portal.GetCitizenByIdAsync(token.CitizenId, ct)
                      ?? throw new NotFoundException("Citizen not found.", SevaErrorCodes.Dept.CitizenNotFound);

        await _orchestration.UpsertDeptUserRefAsync(new DeptUserRef
        {
            Id = Guid.NewGuid(),
            DepartmentId = token.DepartmentId,
            CitizenId = citizen.Id,
            OperatorId = token.OperatorId,
            AssistedMode = token.AssistedMode,
            CreatedAt = _clock.UtcNow
        }, ct);

        TokenJourneyLog.Emit(
            _log, "T6", "REDEEM", "Get User Details OK — full profile returned",
            ("sevaOneUserId", token.SevaOneUserId.ToString()), ("dept", request.DepartmentId));

        return _mapper.Map<UserDetailsResult>(citizen) with
        {
            ServiceId = scheme.ServiceCode,
            AssistedMode = token.AssistedMode,
            OperatorId = operatorCode,
            ExpiresAt = token.ExpireAt,
            ReturnUrl = token.ReturnUrl
        };
    }

    /// <summary>
    /// Department -> SevaOne: application done, send the citizen's browser back. Mints a
    /// second, short-lived (10 min), one-time token distinct from the original apply-token
    /// (which was already redeemed at Token Validate / Get User Details time) -- this one's
    /// only job is proving, when the browser lands back on SevaOne, that the department
    /// itself confirmed completion for this specific citizen. Reuses the exact same
    /// mint/encrypt/store machinery as the original apply-token (PortalApplyService),
    /// including the redirect-payload encryption -- department still never decrypts it.
    /// </summary>
    public async Task<CompleteApplicationResult> CompleteApplicationAsync(CompleteApplicationRequest request, CancellationToken ct)
    {
        // Deliberately NOT a thrown NotFoundException: a citizen with no SevaOne relationship
        // at all (direct department login, never went through an apply-token handoff) is a
        // normal, expected case here, not an integration error -- there being nothing to
        // redirect back to is the correct outcome, not a failure the department needs to
        // catch/retry. Linked:false lets their integration code just check a flag.
        var citizen = await _portal.GetCitizenBySevaOneUserIdAsync(request.SevaOneUserId, ct);
        if (citizen is null)
        {
            TokenJourneyLog.Emit(
                _log, "T6", "SKIP", "Apply Complete — no SevaOne citizen linked, nothing to redirect back to",
                ("sevaOneUserId", request.SevaOneUserId.ToString()), ("dept", request.DepartmentId));
            return new CompleteApplicationResult(Linked: false, ReturnUrl: null, ExpiresAt: null);
        }

        var scheme = await _discovery.GetSchemeByDepartmentAndServiceCodeAsync(request.DepartmentId, request.ServiceId, ct)
                     ?? throw new NotFoundException($"Scheme {request.ServiceId} was not found in department {request.DepartmentId}.", SevaErrorCodes.Dept.SchemeNotFound);
        var department = await _discovery.GetDepartmentByCodeAsync(request.DepartmentId, ct)
                          ?? throw new NotFoundException("Department not found.", SevaErrorCodes.Dept.DepartmentNotFound);

        var now = _clock.UtcNow;

        // Nothing else in the redirect journey ever creates the orchestration.applications row
        // Phase 7 tracking (GET /applications, /track/{ref}) and the department-status webhook
        // both depend on -- this is the first point SevaOne actually knows the citizen's
        // application with this department exists. get-acknowledgement can legitimately be called
        // more than once (e.g. the department redirecting a second time after the first
        // completion token expired unused) -- only insert once per citizen+scheme+department,
        // never a duplicate row for the same application.
        var existing = (await _orchestration.GetApplicationsByCitizenAsync(citizen.Id, ct))
            .FirstOrDefault(a => a.SchemeId == scheme.Id && a.DepartmentId == department.Id);
        if (existing is null)
        {
            var year = now.Year;
            var sevaRef = $"SVK{year}{Random.Shared.Next(100000, 999999)}";
            while (await _orchestration.GetApplicationByReferenceAsync(sevaRef, ct) is not null)
            {
                sevaRef = $"SVK{year}{Random.Shared.Next(100000, 999999)}";
            }

            var status = ApplicationStatusMapper.FromDepartment(request.Status);
            var application = new ServiceApplication
            {
                Id = Guid.NewGuid(),
                SevaReference = sevaRef,
                DepartmentReference = request.DepartmentReference,
                CitizenId = citizen.Id,
                SchemeId = scheme.Id,
                DepartmentId = department.Id,
                Status = status,
                SubmittedAt = now,
                SakalaDueAt = now.AddDays(scheme.SakalaDays),
                ApplicantName = citizen.Name,
                Address = citizen.Address ?? string.Empty,
                District = citizen.District,
                Taluk = citizen.Taluk
            };
            await _orchestration.SubmitApplicationAsync(application, new ApplicationEvent
            {
                Id = Guid.NewGuid(),
                Status = status,
                Note = $"Application completed by {department.Code}",
                At = now
            }, uow: null, ct);

            TokenJourneyLog.Emit(
                _log, "T6", "APPLICATION_CREATED", $"orchestration.applications row created (sevaReference={sevaRef})",
                ("sevaOneUserId", request.SevaOneUserId.ToString()), ("dept", request.DepartmentId));
        }

        var expireAt = now.AddMinutes(10);

        var (tokenValue, tokenHash) = _jws.Mint(new JwsClaims(
            SevaOneUserId: citizen.SevaOneUserId,
            DepartmentId: department.Code,
            ServiceId: scheme.ServiceCode,
            AssistedMode: false,
            OperatorId: null,
            IssuedAt: now,
            ExpireAt: expireAt));

        const string returnPath = "/dashboard";
        await _portal.InsertTokenAsync(new SevaOneToken
        {
            Id = Guid.NewGuid(),
            CitizenId = citizen.Id,
            SevaOneUserId = citizen.SevaOneUserId,
            OperatorId = null,
            AssistedMode = false,
            DepartmentId = department.Id,
            SchemeId = scheme.Id,
            TokenValue = tokenValue,
            TokenHash = tokenHash,
            IssuedAt = now,
            ExpireAt = expireAt,
            MidnightValidUntil = null,
            IsActive = true,
            ReturnUrl = returnPath
        }, uow: null, ct);

        TokenJourneyLog.Emit(
            _log, "T6", "COMPLETE", $"Application completion token issued (status={request.Status})",
            ("sevaOneUserId", request.SevaOneUserId.ToString()), ("dept", request.DepartmentId));

        var encryptedToken = _redirectProtector.Protect(tokenValue);
        return new CompleteApplicationResult(
            Linked: true,
            ReturnUrl: $"/api/v1/apply/return?token={Uri.EscapeDataString(encryptedToken)}",
            ExpiresAt: expireAt);
    }

    /// <summary>Payment Status POST: department fee completion callback. Token rule here is deliberately NOT the 30-minute session rule — valid until midnight of the same calendar day the token was issued, since fee payment can happen well after the citizen's active session ended.</summary>
    public async Task<PostPaymentStatusResult> PostPaymentStatusAsync(PostPaymentStatusRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<PaymentPostStatus>(request.PaymentStatus, ignoreCase: true, out var status))
        {
            throw new DomainException($"Unknown payment status '{request.PaymentStatus}'.", SevaErrorCodes.Dept.UnknownPaymentStatus);
        }

        var resolution = await _gate.ResolveAsync(request.DepartmentId, request.Token, ct);
        if (!resolution.Success || resolution.Token is not { } token)
        {
            throw new ForbiddenException("Token is invalid or does not belong to this department.", SevaErrorCodes.Dept.TokenInvalid);
        }

        var now = _clock.UtcNow;
        var validTill = token.MidnightValidUntil
                         ?? throw new ForbiddenException("Token has no payment-status validity window.", SevaErrorCodes.Dept.PaymentWindowMissing);
        if (now > validTill)
        {
            throw new ForbiddenException("Token is past its midnight validity window for Payment Status POST.", SevaErrorCodes.Dept.PaymentWindowExpired);
        }

        // Idempotent on the department's own retry key — a webhook-style POST must not
        // double-record a status that's already been posted.
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await _status.GetPaymentStatusByIdempotencyKeyAsync(request.IdempotencyKey, ct);
            if (existing is not null)
            {
                return new PostPaymentStatusResult(Accepted: true, Status: existing.PaymentStatus);
            }
        }

        await _status.InsertPaymentStatusPostAsync(new PaymentStatusPost
        {
            Id = Guid.NewGuid(),
            CitizenId = token.CitizenId,
            DepartmentId = token.DepartmentId,
            SchemeId = token.SchemeId,
            SevaOneTokenId = token.Id,
            TokenValue = request.Token,
            TokenHashAtPost = _jws.ComputeHash(request.Token),
            PaymentStatus = status,
            Amount = request.Amount,
            PostedAt = now,
            TokenValidTill = validTill,
            IdempotencyKey = request.IdempotencyKey
        }, ct);

        return new PostPaymentStatusResult(Accepted: true, Status: status);
    }
}
