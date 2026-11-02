using System.Text.Json;
using SevaOne.Business.Portal.DTOs;
using SevaOne.Business.Portal.Interfaces;
using SevaOne.Business.Common;
using SevaOne.Business.Discovery.Interfaces;
using SevaOne.Business.SevaSindhu.DTOs;
using SevaOne.Business.SevaSindhu.Interfaces;
using SevaOne.Domain.Common;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Audit;
using SevaOne.Infrastructure.Repositories.Discovery;
using SevaOne.Infrastructure.Repositories.Portal;
using SevaOne.Infrastructure.Security;

namespace SevaOne.Business.Portal.Services;

public sealed class PortalApplyService : IPortalApplyService
{
    /// <summary>
    /// The one seeded test citizen (see R__seed_test_citizens.sql) that CreateTestApplyTokenAsync
    /// always mints for. Two more test citizens are seeded alongside it for future use, but
    /// nothing wires them up yet -- this is the only one this endpoint uses today.
    /// </summary>
    private const string TestCitizenWso2Subject = "test-citizen-dept-integration-01";

    private readonly IPortalRepository _portal;
    private readonly IDiscoveryRepository _discovery;
    private readonly IAuditRepository _audit;
    private readonly ICurrentCitizen _current;
    private readonly ICurrentOperator _currentOperator;
    private readonly IJwsTokenService _jws;
    private readonly IApplyRedirectProtector _redirectProtector;
    private readonly IAjskRedirectService _ajskRedirect;
    private readonly ISevaSindhuCatalog2Service _sakalaTokenRedirect;
    private readonly ISevaSindhuEscomService _sakalaTokenEscomRedirect;
    private readonly IClock _clock;
    private readonly ITokenIntegrationRepository _tokenIntegrations;
    private readonly IKaveriSsoService _kaveriSso;
    private readonly IEdcsApiTokenService _edcsApi;
    private readonly IEdcsApiFormTokenService _edcsApiForm;

    public PortalApplyService(
        IPortalRepository portal,
        IDiscoveryRepository discovery,
        IAuditRepository audit,
        ICurrentCitizen current,
        ICurrentOperator currentOperator,
        IJwsTokenService jws,
        IApplyRedirectProtector redirectProtector,
        IAjskRedirectService ajskRedirect,
        ISevaSindhuCatalog2Service sakalaTokenRedirect,
        ISevaSindhuEscomService sakalaTokenEscomRedirect,
        IClock clock,
        ITokenIntegrationRepository tokenIntegrations,
        IKaveriSsoService kaveriSso,
        IEdcsApiTokenService edcsApi,
        IEdcsApiFormTokenService edcsApiForm)
    {
        _portal = portal;
        _discovery = discovery;
        _audit = audit;
        _current = current;
        _currentOperator = currentOperator;
        _jws = jws;
        _redirectProtector = redirectProtector;
        _ajskRedirect = ajskRedirect;
        _sakalaTokenRedirect = sakalaTokenRedirect;
        _sakalaTokenEscomRedirect = sakalaTokenEscomRedirect;
        _tokenIntegrations = tokenIntegrations;
        _kaveriSso = kaveriSso;
        _edcsApi = edcsApi;
        _edcsApiForm = edcsApiForm;
        _clock = clock;
    }

    /// <summary>
    /// Citizen self-service path. Deliberately does NOT require ICurrentCitizen upfront -- a
    /// plain homepage-fallback service (no real deep link, see ApplyRouteClassifier) needs no
    /// identity at all, so an anonymous citizen resolves it here directly with no login and no
    /// audit entry (nothing personal to record for a click nobody was identified for). Only a
    /// real deep link, which must embed SevaOneUserId, ever needs a logged-in citizen -- that
    /// case returns RequiresLogin:true instead of throwing, so the caller can redirect to login
    /// and simply call this endpoint again afterward.
    /// </summary>
    public async Task<ApplyOutcomeDto> CreateApplyTokenAsync(CreateApplyTokenRequest request, CancellationToken ct)
    {
        var (scheme, department) = await ResolveActiveSchemeAsync(request.DepartmentCode, request.ServiceCode, ct);

        Citizen? citizen = null;
        if (_current.CitizenId is { } citizenId)
        {
            citizen = await _portal.GetCitizenByIdAsync(citizenId, ct)
                      ?? throw new NotFoundException("Citizen not found.");
        }

        // Admin-configured token integration (Lane 2) -- citizens only. Checked before the Sakala /
        // deep-link routes below, which are left exactly as they were. A service with no enabled
        // setup falls straight through to the existing behaviour.
        var tokenIntegration = await _tokenIntegrations.GetEnabledAsync(scheme.Id, ct);
        if (tokenIntegration is { TypeKey: "seva_sindhu_v3" or "kaveri_sso" or "edcs_api" or "edcs_api_form" })
        {
            if (citizen is null)
            {
                return new ApplyOutcomeDto(RequiresLogin: true, ApplyUrl: null, ExpiresAt: null, IsHomepageFallback: false);
            }

            // A citizen can leave before finishing the profile, so there may be no email yet. Ask once, keep it for next time.
            if (string.IsNullOrWhiteSpace(citizen.Email))
            {
                var given = request.Answers is not null && request.Answers.TryGetValue("Email", out var e) ? e.Trim() : null;
                if (given is null || !System.Net.Mail.MailAddress.TryCreate(given, out var addr) || addr.Address != given)
                {
                    var asks = new List<ApplyInputRequestDto> { new("Email", given is null ? "Email address" : "Enter a valid email address") };
                    // One dialog: the service's own questions are asked together with the email.
                    if (tokenIntegration.TypeKey is "edcs_api" or "edcs_api_form")
                        asks.AddRange(SevaOne.Business.Discovery.Services.EdcsKeyValue.Build(SevaOne.Business.Discovery.Services.EdcsKeyValue.RawKeys(tokenIntegration.ExtraConfig), citizen, request.Answers).Needs);
                    return new ApplyOutcomeDto(RequiresLogin: false, ApplyUrl: null, ExpiresAt: null, IsHomepageFallback: false, NeedsInput: asks);
                }
                citizen.Email = given;
                citizen.UpdatedAt = _clock.UtcNow;
                await _portal.UpdateCitizenAsync(citizen, ct);
            }

            // Single sign-on: Kaveri gives us the address to send the citizen to (a normal link, opened in a new tab).
            if (tokenIntegration.TypeKey == "kaveri_sso")
            {
                var signOnUrl = await _kaveriSso.StartAsync(citizen, scheme, department, tokenIntegration, ct);
                return new ApplyOutcomeDto(RequiresLogin: false, ApplyUrl: signOnUrl, ExpiresAt: null, IsHomepageFallback: false);
            }

            // The department checks tokens against Seva Sindhu's own API, so the token comes from there.
            // "edcs_api": Seva Sindhu's own /redirect sends the citizen on. "edcs_api_form": we post the
            // token to the department's URL ourselves (the original method) -- faster, one less hop.
            if (tokenIntegration.TypeKey == "edcs_api")
                return await _edcsApi.StartAsync(citizen, scheme, department, tokenIntegration, ct, request.Answers);
            if (tokenIntegration.TypeKey == "edcs_api_form")
                return await _edcsApiForm.StartAsync(citizen, scheme, department, tokenIntegration, ct, request.Answers);

            return await StartTokenHandoffAsync(citizen, scheme, department, tokenIntegration, ct);
        }

        if (citizen is null)
        {
            var route = ApplyRouteClassifier.Classify(scheme, department, assistedMode: false);
            if (route != ApplyRoute.HomepageFallback)
            {
                return new ApplyOutcomeDto(RequiresLogin: true, ApplyUrl: null, ExpiresAt: null, IsHomepageFallback: false);
            }

            // HomepageFallback never needs a token -- resolve it directly, no MintApplyTokenAsync
            // call (and so no audit entry, deliberately: there's no citizen to attribute one to).
            return new ApplyOutcomeDto(RequiresLogin: false, ApplyRouteClassifier.FallbackUrl(scheme, assistedMode: false), ExpiresAt: null, IsHomepageFallback: true);
        }

        var minted = await MintApplyTokenAsync(citizen, scheme, department, assistedMode: false, operatorGuid: null, operatorCode: null, testMode: false, ct);
        return new ApplyOutcomeDto(RequiresLogin: false, minted.ApplyUrl, minted.ExpiresAt, minted.IsHomepageFallback);
    }

    /// <summary>
    /// Token integration (Seva Sindhu V3), step 2 of the contract: mint a single-use token, allocate
    /// the unique edcsApplicationRefNo, record the attempt, and hand the browser the fields to POST
    /// to the department. The token is the same signed, encrypted, 30-minute, single-redemption
    /// token the existing department flow uses.
    /// </summary>
    private async Task<ApplyOutcomeDto> StartTokenHandoffAsync(
        Citizen citizen, Scheme scheme, Department department, TokenIntegrationActiveRow integration, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var expireAt = now.AddMinutes(30);

        var (tokenValue, tokenHash) = _jws.Mint(new JwsClaims(
            SevaOneUserId: citizen.SevaOneUserId,
            DepartmentId: department.Code,
            ServiceId: scheme.ServiceCode,
            AssistedMode: false,
            OperatorId: null,
            IssuedAt: now,
            ExpireAt: expireAt));

        var tokenId = Guid.NewGuid();
        await _portal.InsertTokenAsync(new SevaOneToken
        {
            Id = tokenId,
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
            MidnightValidUntil = new DateTimeOffset(now.UtcDateTime.Date.AddDays(1), TimeSpan.Zero),
            IsActive = true,
            ReturnUrl = "/dashboard"
        }, uow: null, ct);

        // ServiceId is the department's own id for the service when we have it (the live catalogue's
        // service code), otherwise our code.
        var fields = new Dictionary<string, string>
        {
            ["UserId"] = citizen.SevaOneUserId.ToString(),
            ["assistedmode"] = "false",
            ["ServiceId"] = string.IsNullOrWhiteSpace(scheme.DepartmentServiceId) ? scheme.ServiceCode : scheme.DepartmentServiceId!,
        };
        if (integration.SendMobile && !string.IsNullOrWhiteSpace(citizen.Mobile)) fields["mobile"] = citizen.Mobile!;
        if (integration.SendDigilockerId && !string.IsNullOrWhiteSpace(citizen.DigiLockerId)) fields["digilockerid"] = citizen.DigiLockerId!;

        var edcsRef = await _tokenIntegrations.CreateAttemptAsync(new TokenIntegrationAttemptWrite
        {
            ServiceId = scheme.Id,
            DepartmentId = department.Id,
            CitizenId = citizen.Id,
            SevaOneUserId = citizen.SevaOneUserId,
            TokenId = tokenId,
            AssistedMode = false,
            LandingUrl = integration.LandingUrl!,
            SentFields = JsonSerializer.Serialize(fields),
            ExpiresAt = expireAt,
        }, ct);

        fields["edcsApplicationRefNo"] = edcsRef;
        fields["Token"] = _redirectProtector.Protect(tokenValue);

        await _audit.InsertAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            CitizenId = citizen.Id,
            Action = "apply.token_integration.started",
            Resource = $"{scheme.Name} · {department.Name}",
            Detail = $"Token integration handoff started, application reference {edcsRef}",
            IpAddress = _current.IpAddress,
            At = now
        }, ct);

        return new ApplyOutcomeDto(RequiresLogin: false, ApplyUrl: null, ExpiresAt: expireAt, IsHomepageFallback: false,
            PostForm: new ApplyPostFormDto(integration.LandingUrl!, fields));
    }

    public async Task<ApplyTokenDto> CreateAssistedApplyTokenAsync(CreateAssistedApplyTokenRequest request, CancellationToken ct)
    {
        if (_currentOperator.OperatorId is not { } operatorId || _currentOperator.OperatorCode is not { } operatorCode)
        {
            throw new ForbiddenException("Not authenticated as an operator.");
        }

        var citizen = await _portal.GetCitizenBySevaOneUserIdAsync(request.SevaOneUserId, ct)
                      ?? throw new NotFoundException($"No citizen found for SevaOneUserID {request.SevaOneUserId}.");
        var (scheme, department) = await ResolveActiveSchemeAsync(request.DepartmentCode, request.ServiceCode, ct);

        return await MintApplyTokenAsync(citizen, scheme, department, assistedMode: true, operatorGuid: operatorId, operatorCode: operatorCode, testMode: false, ct);
    }

    /// <summary>
    /// Department integration testing only. No citizen/operator session -- the caller is a
    /// department, already authenticated by DeptController's usual ApiKey + DeptMachineAuth
    /// three-layer trust. DepartmentId on the request is that authenticated caller, never raw
    /// body input; used here only to refuse minting a token for a service that isn't theirs.
    /// </summary>
    public async Task<ApplyTokenDto> CreateTestApplyTokenAsync(CreateTestApplyTokenRequest request, CancellationToken ct)
    {
        var citizen = await _portal.GetCitizenByWso2SubjectAsync(TestCitizenWso2Subject, ct)
                      ?? throw new NotFoundException("Seeded test citizen not found -- has R__seed_test_citizens.sql run?");

        // request.DepartmentId is the authenticated caller (DeptController.TryResolveDepartment)
        // -- looking the scheme up scoped to it means a service code that only exists under a
        // different department 404s outright, instead of finding that department's service and
        // then having to check ownership after the fact.
        var (scheme, department) = await ResolveActiveSchemeAsync(request.DepartmentId, request.ServiceCode, ct);

        return await MintApplyTokenAsync(citizen, scheme, department, assistedMode: false, operatorGuid: null, operatorCode: null, testMode: true, ct);
    }

    private async Task<(Scheme Scheme, Department Department)> ResolveActiveSchemeAsync(string departmentCode, string serviceCode, CancellationToken ct)
    {
        var scheme = await _discovery.GetSchemeByDepartmentAndServiceCodeAsync(departmentCode, serviceCode, ct)
                     ?? throw new NotFoundException($"Scheme {serviceCode} was not found in department {departmentCode}.");
        var department = await _discovery.GetDepartmentByIdAsync(scheme.DepartmentId, ct)
                          ?? throw new NotFoundException("Department not found.");

        if (!scheme.IsActive || !department.IsActive)
        {
            throw new ConflictException($"Service {serviceCode} is not currently active.");
        }

        return (scheme, department);
    }

    private async Task<ApplyTokenDto> MintApplyTokenAsync(
        Citizen citizen, Scheme scheme, Department department, bool assistedMode, Guid? operatorGuid, string? operatorCode, bool testMode, CancellationToken ct)
    {
        var serviceCode = scheme.ServiceCode;
        var route = ApplyRouteClassifier.Classify(scheme, department, assistedMode);

        // Operator-assisted apply on a service flagged for a special integration mechanism
        // bypasses the normal deep-link/JWS flow entirely -- AJSK is a redirect into a
        // different government system with its own token-free URL shape (see
        // AjskRedirectOptions), not something SevaOne mints a token for. Citizen apply on the
        // same service is untouched: IntegrationMethod is only ever consulted when
        // assistedMode is true.
        //
        // UserId is the ASSISTING OPERATOR's own whitelist id (operatorCode), not the citizen's
        // -- confirmed directly: the integration doc's sample UserId (32487163) is a real row
        // in portal.operator_whitelist (registration BLS123, kiosk type SPEW), not a citizen
        // record. operatorCode is guaranteed non-null here: this branch only runs when
        // assistedMode is true, which CreateAssistedApplyTokenAsync only sets after confirming
        // the caller is an authenticated operator.
        if (route == ApplyRoute.AjskRedirect)
        {
            var ajskUrl = _ajskRedirect.BuildRedirectUrl(operatorCode!);

            await _audit.InsertAsync(new AuditLog
            {
                Id = Guid.NewGuid(),
                CitizenId = citizen.Id,
                Action = "apply.ajsk_redirect.assisted",
                // Human-readable for the citizen's Recent Activity feed (frontend
                // auditLabels.ts). The machine codes still live on the minted JWS and in
                // discovery.service_apply_track; nothing reads them back off the audit row.
                Resource = $"{scheme.Name} · {department.Name}",
                Detail = $"AJSK redirect issued for department apply handoff (assisted by operator {operatorCode})",
                IpAddress = _current.IpAddress ?? _currentOperator.IpAddress,
                At = _clock.UtcNow
            }, ct);

            return new ApplyTokenDto(ajskUrl, ExpiresAt: null, IsHomepageFallback: false);
        }

        // Same idea, the Sakala Token Generation API mechanism instead of AJSK's plain
        // redirect. Two variants share this branch: 'sakala_token' (generic Sakala flow,
        // reuses ISevaSindhuCatalog2Service -- already live for Catalog 2's "Apply now") and
        // 'sakala_token_escom' (BESCOM/CESCOM/MESCOM's own token/redirect endpoints, reuses
        // ISevaSindhuEscomService) -- same request/response shape, different external API, so
        // neither is duplicated here. Only ever reached with a real SevasindhuServiceId: the
        // flag is set exclusively for services that were actually mapped to one (see
        // V60__sevasindhu_service_id_mapping.sql, V61__operator_only_sakala_services.sql),
        // never speculatively.
        if (route == ApplyRoute.SakalaTokenRedirect)
        {
            var sevasindhuServiceId = scheme.SevasindhuServiceId!;
            var redirectUrl = scheme.IntegrationMethod == "sakala_token_escom"
                ? (await _sakalaTokenEscomRedirect.GenerateRedirectAsync(new GenerateEscomRedirectRequest(sevasindhuServiceId), ct)).RedirectUrl
                : (await _sakalaTokenRedirect.GenerateRedirectAsync(new GenerateCatalog2RedirectRequest(sevasindhuServiceId), ct)).RedirectUrl;

            await _audit.InsertAsync(new AuditLog
            {
                Id = Guid.NewGuid(),
                CitizenId = citizen.Id,
                Action = "apply.sakala_token_redirect.assisted",
                Resource = $"{scheme.Name} · {department.Name}",
                Detail = $"Sakala token redirect ({scheme.IntegrationMethod}) issued for department apply handoff (assisted by operator {operatorCode})",
                IpAddress = _current.IpAddress ?? _currentOperator.IpAddress,
                At = _clock.UtcNow
            }, ct);

            return new ApplyTokenDto(redirectUrl, ExpiresAt: null, IsHomepageFallback: false);
        }

        if (route == ApplyRoute.HomepageFallback)
        {
            // Either no department-side apply endpoint is configured for this audience, or the
            // department isn't actually integrated for token redemption at all (see
            // ApplyRouteClassifier) -- either way, nothing on the other end would ever redeem a
            // minted token, so just send the citizen straight to the best URL available
            // (deeplink when the catalog import populated one, otherwise the plain homepage).
            var fallbackUrl = ApplyRouteClassifier.FallbackUrl(scheme, assistedMode);
            if (string.IsNullOrWhiteSpace(fallbackUrl))
            {
                throw new ConflictException($"Service {serviceCode} has no Apply URL or homepage configured yet.");
            }

            await _audit.InsertAsync(new AuditLog
            {
                Id = Guid.NewGuid(),
                CitizenId = citizen.Id,
                Action = testMode ? "apply.homepage_fallback.test" : assistedMode ? "apply.homepage_fallback.assisted" : "apply.homepage_fallback",
                Resource = $"{scheme.Name} · {department.Name}",
                Detail = department.IsIntegrated
                    ? "No deep link configured for this department/service -- redirected to homepage, no token minted"
                    : "Department is not integrated for token redemption -- redirected directly, no token minted",
                IpAddress = _current.IpAddress ?? _currentOperator.IpAddress,
                At = _clock.UtcNow
            }, ct);

            // Click counting no longer happens here -- see ApplyClickTrackingService. This
            // branch is only ever reached with a resolved citizen anyway (CreateApplyTokenAsync
            // resolves HomepageFallback directly, without calling this method, for the
            // anonymous case), so removing this was purely moving the write, not a behavior
            // change for this specific branch.

            return new ApplyTokenDto(fallbackUrl, ExpiresAt: null, IsHomepageFallback: true);
        }

        // route == ApplyRoute.RealDeeplinkMint -- department.IsIntegrated is true and a real
        // deep link exists (Classify already confirmed this is non-blank), so this is the only
        // case that actually mints a redeemable token.
        var deeplinkUrl = (assistedMode
            ? (string.IsNullOrWhiteSpace(scheme.DeeplinkOperatorUrl) ? scheme.DeeplinkCitizenUrl : scheme.DeeplinkOperatorUrl)
            : scheme.DeeplinkCitizenUrl)!;

        var now = _clock.UtcNow;
        var expireAt = now.AddMinutes(30);
        // "Same calendar day" — computed in UTC pending a product decision on which timezone
        // governs "midnight" (IST is the obvious candidate for a Karnataka system). Revisit
        // before this rule is relied on.
        var midnightValidUntil = new DateTimeOffset(now.UtcDateTime.Date.AddDays(1), TimeSpan.Zero);

        var (tokenValue, tokenHash) = _jws.Mint(new JwsClaims(
            SevaOneUserId: citizen.SevaOneUserId,
            DepartmentId: department.Code,
            ServiceId: scheme.ServiceCode,
            AssistedMode: assistedMode,
            OperatorId: operatorCode,
            IssuedAt: now,
            ExpireAt: expireAt));

        await _portal.InsertTokenAsync(new SevaOneToken
        {
            Id = Guid.NewGuid(),
            CitizenId = citizen.Id,
            SevaOneUserId = citizen.SevaOneUserId,
            OperatorId = operatorGuid,
            AssistedMode = assistedMode,
            DepartmentId = department.Id,
            SchemeId = scheme.Id,
            TokenValue = tokenValue,
            TokenHash = tokenHash,
            IssuedAt = now,
            ExpireAt = expireAt,
            MidnightValidUntil = midnightValidUntil,
            IsActive = true,
            // Where PortalDeptFacingService.GetUserDetailsAsync hands this back to the
            // department, for it to redirect the citizen's browser to once the department calls
            // CompleteApplicationAsync. Fixed for now -- no product need yet for a
            // citizen-chosen override.
            ReturnUrl = "/dashboard"
        }, uow: null, ct);

        await _audit.InsertAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            CitizenId = citizen.Id,
            Action = testMode ? "apply.token.issued.test" : assistedMode ? "apply.token.issued.assisted" : "apply.token.issued",
            Resource = $"{scheme.Name} · {department.Name}",
            Detail = testMode
                ? "SevaOne JWS minted for department integration testing (fixed seeded test citizen, no citizen/operator session)"
                : assistedMode
                    ? $"SevaOne JWS minted for department apply handoff (assisted by operator {operatorCode})"
                    : "SevaOne JWS minted for department apply handoff",
            IpAddress = _current.IpAddress ?? _currentOperator.IpAddress,
            At = now
        }, ct);

        // Click counting no longer happens here -- see ApplyClickTrackingService. Also fixes,
        // as a side effect, that the AJSK/Sakala-token branches above never counted a click at
        // all: every route now counts through that one shared entry point instead.

        // The URL carries ONLY the encrypted token -- no DepartmentID/ServiceID/SevaOneUserID in
        // plain query params (a government-official-flagged requirement: those three fields
        // used to travel in the clear here). The department already gets all of that back from
        // /dept/user-details's response after redeeming the token, so nothing depends on reading
        // them off the URL. The token itself is still only signed (tamper-evident), not
        // confidential -- the DB row above stores/hashes the plaintext JWS; DeptTokenGate.
        // ResolveAsync decrypts this back to that same plaintext before doing the hash lookup, so
        // the department never needs to understand or decrypt it -- it just relays this opaque
        // string back to SevaOne's own /api/v1/dept/* endpoints, same as before.
        var encryptedToken = _redirectProtector.Protect(tokenValue);
        var separator = deeplinkUrl.Contains('?') ? '&' : '?';
        var applyUrl = $"{deeplinkUrl}{separator}Token={Uri.EscapeDataString(encryptedToken)}";

        return new ApplyTokenDto(applyUrl, expireAt, IsHomepageFallback: false);
    }

    /// <summary>
    /// Resolves the department's return-redirect (the completion token minted by
    /// PortalDeptFacingService.CompleteApplicationAsync). Order matters: signature/expiry/
    /// existence checks, THEN the atomic one-time redeem, THEN the session cross-check --
    /// redeeming before the cross-check means a mismatched/missing session still permanently
    /// consumes the token (standard one-time-token semantics: this URL is meant to be hit
    /// exactly once, by the citizen's own browser, as part of the actual redirect).
    /// </summary>
    public async Task<ApplyReturnResult> CompleteReturnAsync(string encryptedToken, CancellationToken ct)
    {
        var now = _clock.UtcNow;

        var tokenValue = _redirectProtector.TryUnprotect(encryptedToken);
        if (tokenValue is null)
        {
            await AuditReturn(null, "apply.return.decrypt_failed", null, now, ct);
            return new ApplyReturnResult("/dashboard?applyError=invalid_token", false);
        }

        var hash = _jws.ComputeHash(tokenValue);
        var token = await _portal.GetTokenByHashAsync(hash, ct);
        if (token is null)
        {
            await AuditReturn(null, "apply.return.token_not_found", null, now, ct);
            return new ApplyReturnResult("/dashboard?applyError=invalid_token", false);
        }

        if (!token.IsActive || token.RevokedAt is not null)
        {
            await AuditReturn(token.CitizenId, "apply.return.already_used", token.Id, now, ct);
            return new ApplyReturnResult("/dashboard?applyError=invalid_token", false);
        }

        if (now > token.ExpireAt)
        {
            await AuditReturn(token.CitizenId, "apply.return.expired", token.Id, now, ct);
            return new ApplyReturnResult("/dashboard?applyError=token_expired", false);
        }

        if (!_jws.VerifySignature(tokenValue))
        {
            await AuditReturn(token.CitizenId, "apply.return.signature_invalid", token.Id, now, ct);
            return new ApplyReturnResult("/dashboard?applyError=invalid_token", false);
        }

        var redeemed = await _portal.TryRedeemTokenAsync(token.Id, now, ct);
        if (!redeemed)
        {
            // Lost a race against a concurrent redeem of the same token -- functionally
            // identical to "already used" from this caller's point of view.
            await AuditReturn(token.CitizenId, "apply.return.already_used", token.Id, now, ct);
            return new ApplyReturnResult("/dashboard?applyError=invalid_token", false);
        }

        if (_current.CitizenId is not { } currentCitizenId)
        {
            // No active SevaOne session on this browser (e.g. it expired while the citizen
            // was on the department's site). The completion token alone is deliberately NOT
            // enough to establish a session -- send them to log back in, then land where the
            // department intended.
            await AuditReturn(token.CitizenId, "apply.return.no_session", token.Id, now, ct);
            return new ApplyReturnResult("/login?returnTo=" + Uri.EscapeDataString(token.ReturnUrl ?? "/dashboard"), false);
        }

        if (currentCitizenId != token.CitizenId)
        {
            // The token was genuine and freshly redeemed, but for a DIFFERENT citizen than
            // whoever's browser is currently sitting on this session -- this is the actual
            // attack case (someone got hold of a completion redirect meant for someone else)
            // rather than an expired-session edge case. Flag distinctly in the audit trail.
            await AuditReturn(token.CitizenId, "apply.return.citizen_mismatch", token.Id, now, ct,
                detail: $"session citizen {currentCitizenId} does not match token citizen {token.CitizenId}");
            return new ApplyReturnResult("/dashboard?applyError=identity_mismatch", false);
        }

        await AuditReturn(token.CitizenId, "apply.return.success", token.Id, now, ct);
        return new ApplyReturnResult(token.ReturnUrl ?? "/dashboard", true);
    }

    private Task AuditReturn(Guid? citizenId, string action, Guid? tokenId, DateTimeOffset at, CancellationToken ct, string? detail = null) =>
        _audit.InsertAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            CitizenId = citizenId,
            Action = action,
            Resource = tokenId?.ToString() ?? "unknown",
            Detail = detail,
            IpAddress = _current.IpAddress,
            At = at
        }, ct);
}
