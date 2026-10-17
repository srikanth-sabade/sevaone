using SevaOne.Business.Portal.DTOs;
using SevaOne.Business.Portal.Interfaces;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Microsoft.Extensions.Logging;
using SevaOne.Business.Common;
using SevaOne.Domain.Common;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Caching;
using SevaOne.Infrastructure.Connectors;
using SevaOne.Infrastructure.ExternalAuth;
using SevaOne.Infrastructure.Repositories.Audit;
using SevaOne.Infrastructure.Repositories.Discovery;
using SevaOne.Infrastructure.Repositories.Orchestration;
using SevaOne.Infrastructure.Repositories.Portal;
using SevaOne.Infrastructure.Security;

namespace SevaOne.Business.Portal.Services;

public sealed class PortalAuthService : IPortalAuthService
{
    private readonly IWso2CitizenIdentityClient _wso2Identity;
    private readonly IWso2TokenIntrospectionCache _introspectionCache;
    private readonly IDigiLockerClient _digiLocker;
    private readonly ISmsOtpSender _smsOtp;
    private readonly ISevaSessionStore _sessions;
    private readonly ICitizenSessionService _citizenSessions;
    private readonly IPortalRepository _portal;
    private readonly IOrchestrationRepository _orchestration;
    private readonly IAuditRepository _audit;
    private readonly IDiscoveryRepository _discovery;
    private readonly IDepartmentConnectorRegistry _connectors;
    private readonly IPayloadProtector _protector;
    private readonly IClock _clock;
    private readonly ICurrentCitizen _current;
    private readonly IMapper _mapper;
    private readonly ILogger<PortalAuthService> _log;

    public PortalAuthService(
        IWso2CitizenIdentityClient wso2Identity,
        IWso2TokenIntrospectionCache introspectionCache,
        IDigiLockerClient digiLocker,
        ISmsOtpSender smsOtp,
        ISevaSessionStore sessions,
        ICitizenSessionService citizenSessions,
        IPortalRepository portal,
        IOrchestrationRepository orchestration,
        IAuditRepository audit,
        IDiscoveryRepository discovery,
        IDepartmentConnectorRegistry connectors,
        IPayloadProtector protector,
        IClock clock,
        ICurrentCitizen current,
        IMapper mapper,
        ILogger<PortalAuthService> log)
    {
        _wso2Identity = wso2Identity;
        _introspectionCache = introspectionCache;
        _digiLocker = digiLocker;
        _smsOtp = smsOtp;
        _sessions = sessions;
        _citizenSessions = citizenSessions;
        _portal = portal;
        _orchestration = orchestration;
        _audit = audit;
        _discovery = discovery;
        _connectors = connectors;
        _protector = protector;
        _clock = clock;
        _current = current;
        _mapper = mapper;
        _log = log;
    }

    public async Task<StartDigiLockerLoginResult> StartDigiLockerLoginAsync(StartDigiLockerLoginRequest request, CancellationToken ct)
    {
        var flow = request.Flow == "signup" ? "signup" : "signin";
        string? pendingMobile = null;
        if (!string.IsNullOrWhiteSpace(request.PendingId))
        {
            var pending = await _sessions.TakeLoginStateAsync(request.PendingId, ct)
                          ?? throw new ForbiddenException("DigiLocker signup challenge expired. Verify mobile OTP again.");
            pendingMobile = pending.PendingMobile;
        }

        var appParam = Guid.NewGuid().ToString("N");
        await _sessions.SaveLoginStateAsync(
            appParam,
            new LoginState { ReturnTo = string.IsNullOrWhiteSpace(request.ReturnTo) ? "/" : request.ReturnTo, PendingMobile = pendingMobile },
            TimeSpan.FromMinutes(15),
            ct);

        var redirectUrl = await _digiLocker.StartSignInAsync(appParam, flow, ct);
        return new StartDigiLockerLoginResult(redirectUrl);
    }

    /// <summary>
    /// Browser lands here after CeG/DigiLocker redirect (query txnid + appparam). AppParam is
    /// consumed once from Redis so a replayed callback cannot mint a second session. Aadhaar is
    /// a soft-fail enrichment via GetEAadharDetails.
    /// </summary>
    public async Task<CompleteLoginResult> CompleteDigiLockerLoginAsync(CompleteDigiLockerLoginRequest request, CancellationToken ct)
    {
        if (!string.Equals(request.Status, "success", StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("DigiLocker sign-in was not successful.");
        }

        if (string.IsNullOrWhiteSpace(request.TxnId) || string.IsNullOrWhiteSpace(request.AppParam))
        {
            throw new ForbiddenException("DigiLocker callback is missing txnid or appparam.");
        }

        var state = await _sessions.TakeLoginStateAsync(request.AppParam, ct)
                    ?? throw new ForbiddenException("DigiLocker sign-in expired. Please try again.");

        var profile = await _digiLocker.GetProfileFromTxnAsync(request.AppParam, request.TxnId, ct);
        var aadhaar = await _digiLocker.GetEAadhaarDetailsAsync(profile.Id, ct);
        var mobile = string.IsNullOrWhiteSpace(state.PendingMobile) ? profile.Mobile : state.PendingMobile;
        // Aadhaar's signed KYC record (when available) is the verified, authoritative source for
        // name/dob/gender -- the basic profile call often comes back with these empty. Prefer it,
        // fall back to the profile call only when Aadhaar didn't return one either.
        var user = new Wso2UserInfo
        {
            Subject = "digilocker:" + profile.Id,
            DigiLockerId = profile.Id,
            Username = string.IsNullOrWhiteSpace(mobile) ? profile.Id : mobile,
            Name = string.IsNullOrWhiteSpace(aadhaar?.Name) ? profile.Name : aadhaar.Name,
            Email = profile.Email,
            Mobile = mobile,
            DateOfBirth = aadhaar?.DateOfBirth ?? profile.DateOfBirth,
            Gender = string.IsNullOrWhiteSpace(aadhaar?.Gender) ? profile.Gender : aadhaar.Gender,
            MobileVerified = !string.IsNullOrWhiteSpace(mobile),
            AadhaarVerified = aadhaar is not null,
            AadhaarMasked = aadhaar?.AadhaarMasked,
            AadhaarHash = aadhaar?.AadhaarHash,
            Address = aadhaar?.Address,
            District = aadhaar?.District,
            Taluk = aadhaar?.Taluk,
            StateName = aadhaar?.StateName,
            VillageName = aadhaar?.VillageName,
            NameKannada = aadhaar?.NameKannada,
            AddressKannada = aadhaar?.AddressKannada,
            DistrictKannada = aadhaar?.DistrictKannada,
            StateKannada = aadhaar?.StateKannada,
            VillageKannada = aadhaar?.VillageKannada,
            Photo = aadhaar?.Photo
        };

        var established = await _citizenSessions.EstablishAsync(user, state.ReturnTo ?? "/", ct);
        return new CompleteLoginResult(established.Tokens, established.ReturnTo);
    }

    public async Task<IReadOnlyList<DigiLockerIssuedDocumentDto>> ListDigiLockerDocumentsAsync(CancellationToken ct)
    {
        if (_current.CitizenId is not { } id)
        {
            throw new ForbiddenException("Not authenticated.");
        }

        var citizen = await _portal.GetCitizenByIdAsync(id, ct)
                      ?? throw new NotFoundException("Citizen not found.");
        const string prefix = "digilocker:";
        if (string.IsNullOrWhiteSpace(citizen.Wso2Subject) || !citizen.Wso2Subject.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("This account is not linked to DigiLocker.");
        }

        var documents = await _digiLocker.GetIssuedDocListAsync(citizen.Wso2Subject[prefix.Length..], ct);
        return documents.Select(d => new DigiLockerIssuedDocumentDto(
            d.DocumentName,
            d.DocumentType,
            d.DocumentUri,
            d.DocumentDescription,
            d.DocumentIssuer,
            d.DocumentUploadDate)).ToList();
    }

    public async Task<IReadOnlyList<AuditEntryDto>> GetMyAuditAsync(int take, CancellationToken ct)
    {
        if (_current.CitizenId is not { } id)
        {
            throw new ForbiddenException("Not authenticated.");
        }

        var entries = await _audit.GetByCitizenAsync(id, take, ct);

        // Older apply / department-session rows store the machine key "deptCode/serviceCode"
        // (or a bare "deptCode") in Resource; newer apply rows already store
        // "Service Name · Department Name". Resolve the machine form to display names here so
        // the citizen's Recent Activity feed never shows a raw service code like "REVENUE-59".
        var labels = new Dictionary<string, string>(StringComparer.Ordinal);
        var result = new List<AuditEntryDto>(entries.Count);
        foreach (var e in entries)
        {
            var resource = e.Resource;
            if (NeedsResourceLabel(e.Action, resource))
            {
                if (!labels.TryGetValue(resource, out var label))
                {
                    label = await ResolveResourceLabelAsync(resource, ct) ?? resource;
                    labels[resource] = label;
                }
                resource = label;
            }
            result.Add(new AuditEntryDto(e.Id, e.Action, resource, e.Detail, e.At));
        }
        return result;
    }

    private static bool NeedsResourceLabel(string action, string resource)
    {
        if (resource.Length == 0 || resource.Any(char.IsWhiteSpace))
        {
            return false; // empty, or already a "Name · Name" display string
        }
        if (action == "department.session.open")
        {
            return true;
        }
        return action.StartsWith("apply.", StringComparison.Ordinal)
               && !action.StartsWith("apply.return.", StringComparison.Ordinal); // apply.return.* Resource is a token id
    }

    private async Task<string?> ResolveResourceLabelAsync(string resource, CancellationToken ct)
    {
        var slash = resource.IndexOf('/');
        var deptCode = slash >= 0 ? resource[..slash] : resource;
        var serviceCode = slash >= 0 ? resource[(slash + 1)..] : string.Empty;
        if (deptCode.Length == 0)
        {
            return null;
        }

        var scheme = serviceCode.Length > 0
            ? await _discovery.GetSchemeByDepartmentAndServiceCodeAsync(deptCode, serviceCode, ct)
            : null;
        var dept = scheme is not null
            ? await _discovery.GetDepartmentByIdAsync(scheme.DepartmentId, ct)
            : await _discovery.GetDepartmentByCodeAsync(deptCode, ct);

        var deptName = dept?.Name ?? deptCode;
        return scheme is not null ? $"{scheme.Name} · {deptName}" : deptName;
    }

    private static readonly System.Text.RegularExpressions.Regex MobileFormat = new("^[0-9]{10,15}$");

    public async Task<StartMobileOtpResult> StartMobileLoginAsync(StartMobileOtpRequest request, CancellationToken ct)
    {
        var mobile = (request.Mobile ?? "").Trim();
        if (!MobileFormat.IsMatch(mobile))
        {
            throw new DomainException("Enter a valid mobile number.");
        }

        var otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var challengeId = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        await _sessions.SaveLoginStateAsync(
            challengeId,
            new LoginState { PendingMobile = mobile, OtpHash = HashOtp(otp), ReturnTo = "/" },
            TimeSpan.FromMinutes(10),
            ct);

        await _smsOtp.SendOtpAsync(mobile, otp, ct);
        _log.LogInformation("Mobile OTP for {Mobile}: {Otp}", mobile, otp);

        return new StartMobileOtpResult(challengeId);
    }

    public async Task<VerifyMobileOtpResult> VerifyMobileLoginAsync(VerifyMobileOtpRequest request, CancellationToken ct)
    {
        var otp = (request.Otp ?? "").Trim();
        if (otp.Length != 6 || !otp.All(char.IsDigit))
        {
            throw new DomainException("Enter the 6-digit OTP.");
        }

        var state = await _sessions.TakeLoginStateAsync(request.ChallengeId, ct)
                    ?? throw new ForbiddenException("OTP challenge expired. Please try again.");

        if (string.IsNullOrWhiteSpace(state.OtpHash) || string.IsNullOrWhiteSpace(state.PendingMobile))
        {
            throw new ForbiddenException("Invalid OTP challenge.");
        }

        var expected = Convert.FromHexString(state.OtpHash);
        var actual = Convert.FromHexString(HashOtp(otp));
        if (expected.Length != actual.Length || !CryptographicOperations.FixedTimeEquals(expected, actual))
        {
            throw new ForbiddenException("Incorrect OTP. Please try again.");
        }

        var mobile = state.PendingMobile;
        var citizen = await _portal.GetCitizenByMobileAsync(mobile, ct);
        if (citizen is null)
        {
            // No existing citizen with this VERIFIED mobile -- not an error, this is the
            // "new citizen, go through DigiLocker next" branch. Nothing to establish yet.
            var pendingId = Guid.NewGuid().ToString("N");
            await _sessions.SaveLoginStateAsync(
                pendingId,
                new LoginState { PendingMobile = mobile, ReturnTo = "/" },
                TimeSpan.FromMinutes(15),
                ct);
            return new VerifyMobileOtpResult(Found: false, mobile, null, null, pendingId);
        }

        // Returning citizen: rebuild a Wso2UserInfo from their OWN existing record and run it
        // through the exact same EstablishAsync path every login source uses. Since
        // ProfileCompleted is already true for a returning citizen, EstablishAsync's field
        // updates are effectively a no-op here -- this is just the shared session-creation path,
        // not a fresh identity bootstrap.
        var user = new Wso2UserInfo
        {
            Subject = citizen.Wso2Subject,
            Username = citizen.Username,
            Name = citizen.Name,
            Email = citizen.Email,
            Mobile = citizen.Mobile,
            DateOfBirth = citizen.DateOfBirth,
            Gender = citizen.Gender,
            MobileVerified = true
        };

        var established = await _citizenSessions.EstablishAsync(user, "/", ct);
        return new VerifyMobileOtpResult(Found: true, mobile, established.Tokens, established.ReturnTo);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken ct)
    {
        // _current.CitizenId is populated by Wso2CitizenAuthMiddleware off the SAME cookie this
        // request.AccessToken came from -- if the token's still valid, it's already been
        // introspected once this request. If it's already expired/invalid, CitizenId is null and
        // department-session cleanup + the audit row are skipped (nothing to clean up for a
        // credential that was never live), but revoke/cache-invalidate below still run
        // unconditionally -- both are harmless no-ops against an already-dead token.
        if (_current.CitizenId is { } citizenId)
        {
            var deptSessions = await _orchestration.GetActiveDepartmentSessionsByCitizenAsync(citizenId, ct);
            foreach (var deptSession in deptSessions)
            {
                try
                {
                    var handle = System.Text.Json.JsonSerializer.Deserialize<DepartmentSessionHandle>(
                        _protector.Unprotect(deptSession.EncryptedPayload));
                    if (handle is not null)
                    {
                        await _connectors.Get(handle.DepartmentCode).CloseSessionAsync(handle, ct);
                    }
                }
                catch
                {
                    // best-effort revoke
                }

                await _orchestration.ExpireDepartmentSessionAsync(deptSession.Id, _clock.UtcNow, ct);
            }

            await _audit.InsertAsync(new AuditLog
            {
                Id = Guid.NewGuid(),
                CitizenId = citizenId,
                Action = "auth.logout",
                Resource = "session",
                IpAddress = _current.IpAddress,
                At = _clock.UtcNow
            }, ct);
        }

        if (!string.IsNullOrWhiteSpace(request.AccessToken))
        {
            // Two independent cleanups: revoke against WSO2 itself (so the token can't be reused
            // even against a consumer that never asks SevaOne), and purge the introspection
            // cache (Wso2TokenIntrospectionCache) so THIS backend stops trusting it immediately
            // instead of waiting out the cache's own short TTL.
            await _wso2Identity.RevokeAsync(request.AccessToken, ct);
            await _introspectionCache.InvalidateAsync(request.AccessToken, ct);
        }
    }

    public async Task<MeDto> GetMeAsync(CancellationToken ct)
    {
        if (_current.CitizenId is not { } id)
        {
            throw new ForbiddenException("Not authenticated.");
        }

        var citizen = await _portal.GetCitizenByIdAsync(id, ct)
                      ?? throw new NotFoundException("Citizen not found.");
        return _mapper.Map<MeDto>(citizen);
    }

    public async Task<MeDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct)
    {
        if (_current.CitizenId is not { } id)
        {
            throw new ForbiddenException("Not authenticated.");
        }

        var citizen = await _portal.GetCitizenByIdAsync(id, ct)
                      ?? throw new NotFoundException("Citizen not found.");

        var mobileLast4 = request.Mobile.Length >= 4 ? request.Mobile[^4..] : "0000";

        citizen.Name = request.Name.Trim();
        citizen.Mobile = request.Mobile.Trim();
        citizen.Email = string.IsNullOrWhiteSpace(request.Email) ? citizen.Email : request.Email.Trim();
        citizen.Address = request.Address.Trim();
        citizen.District = request.District.Trim();
        citizen.Taluk = request.Taluk.Trim();
        citizen.StateName = request.StateName?.Trim();
        citizen.VillageName = request.VillageName?.Trim();
        citizen.DateOfBirth = request.DateOfBirth;
        citizen.Gender = request.Gender;

        // A citizen who already has a DigiLocker-verified Aadhaar (AadhaarHash set) keeps it --
        // request.AadhaarLast4 is ignored in that case. It's only ever used as the fallback for
        // citizens with no verified Aadhaar source at all (WSO2/password login), same as before
        // this field had a real source to defer to.
        if (string.IsNullOrWhiteSpace(citizen.AadhaarHash))
        {
            var last4 = string.IsNullOrWhiteSpace(request.AadhaarLast4) ? mobileLast4 : request.AadhaarLast4;
            citizen.AadhaarMasked = $"XXXX-XXXX-{last4}";
        }

        citizen.SevaId = $"SO{mobileLast4}KA";
        citizen.ProfileCompleted = true;
        citizen.ProfileFinishPct = ProfileCompleteness.Calculate(citizen);
        citizen.UpdatedAt = _clock.UtcNow;

        await _portal.UpdateCitizenAsync(citizen, ct);

        await _audit.InsertAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            CitizenId = citizen.Id,
            Action = "auth.profile.complete",
            Resource = "citizen",
            Detail = "Registration profile completed",
            IpAddress = _current.IpAddress,
            At = _clock.UtcNow
        }, ct);

        // No session cache to patch anymore -- Wso2CitizenAuthMiddleware loads the Citizen fresh
        // from Postgres on every request (keyed off the WSO2 token's username claim), so a
        // profile edit is visible on the very next request with nothing extra to keep in sync.
        return _mapper.Map<MeDto>(citizen);
    }

    internal static string HashOtp(string otp)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(otp.Trim()));
        return Convert.ToHexString(bytes);
    }
}
