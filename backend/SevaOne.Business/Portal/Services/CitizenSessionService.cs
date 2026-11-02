using SevaOne.Business.Portal.DTOs;
using SevaOne.Business.Portal.Interfaces;
using SevaOne.Business.Common;
using SevaOne.Domain.Common;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.ExternalAuth;
using SevaOne.Infrastructure.Repositories.Audit;
using SevaOne.Infrastructure.Repositories.Orchestration;
using SevaOne.Infrastructure.Repositories.Portal;
using SevaOne.Infrastructure.Security;

namespace SevaOne.Business.Portal.Services;

public sealed class CitizenSessionService : ICitizenSessionService
{
    private readonly IPortalRepository _portal;
    private readonly IOrchestrationRepository _orchestration;
    private readonly IAuditRepository _audit;
    private readonly IClock _clock;
    private readonly ICurrentCitizen _current;
    private readonly IWso2CitizenIdentityClient _wso2Identity;

    public CitizenSessionService(
        IPortalRepository portal,
        IOrchestrationRepository orchestration,
        IAuditRepository audit,
        IClock clock,
        ICurrentCitizen current,
        IWso2CitizenIdentityClient wso2Identity)
    {
        _portal = portal;
        _orchestration = orchestration;
        _audit = audit;
        _clock = clock;
        _current = current;
        _wso2Identity = wso2Identity;
    }

    public async Task<bool?> GetProfileCompletedAsync(string? subject, string? username, CancellationToken ct)
    {
        Citizen? citizen = null;
        if (!string.IsNullOrWhiteSpace(subject))
        {
            citizen = await _portal.GetCitizenByWso2SubjectAsync(subject, ct);
        }

        if (citizen is null && !string.IsNullOrWhiteSpace(username))
        {
            citizen = await _portal.GetCitizenByUsernameAsync(username.Trim(), ct);
        }

        return citizen?.ProfileCompleted;
    }

    public async Task<(Wso2CitizenTokens Tokens, string ReturnTo, bool ProfileCompleted)> EstablishAsync(
        Wso2UserInfo user, string returnTo, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var citizen = await _portal.GetCitizenByWso2SubjectAsync(user.Subject, ct);
        var isNew = citizen is null;
        if (citizen is null)
        {
            // A citizen who already has an account under a DIFFERENT login (e.g. registered
            // via mobile 1234, and their DigiLocker session — perhaps started because a LATER
            // mobile-OTP attempt with a different number 3456 found no account — carries a
            // stable-per-session digilocker: subject that doesn't match their original one)
            // would otherwise silently get a SECOND account here, with the same real Aadhaar
            // duplicated across two SevaOne accounts. Block it instead: this request has
            // already been through real DigiLocker consent, so it's genuinely this citizen,
            // and it's safe to tell them their own existing account's mobile hint back.
            if (user.AadhaarVerified && !string.IsNullOrWhiteSpace(user.AadhaarHash))
            {
                var existing = await _portal.GetCitizenByAadhaarHashAsync(user.AadhaarHash, ct);
                if (existing is not null)
                {
                    var hint = !string.IsNullOrWhiteSpace(existing.Mobile) && existing.Mobile.Length >= 4
                        ? $"ending in {existing.Mobile[^4..]}"
                        : "already on file";
                    throw new ConflictException(
                        $"You already have a SevaOne account registered with a different mobile number ({hint}). Please log in using mobile OTP with that number instead.");
                }
            }

            citizen = new Citizen
            {
                Id = Guid.NewGuid(),
                Wso2Subject = user.Subject,
                Username = string.IsNullOrWhiteSpace(user.Username) ? user.Subject : user.Username,
                // Don't fall back to Username/mobile here -- that made "Full name" show the
                // citizen's own phone number when no real name was available. Leave it genuinely
                // empty instead; the registration form already renders an empty, editable field
                // for whatever didn't come back verified, and the citizen fills it themselves.
                Name = user.Name ?? string.Empty,
                Email = user.Email,
                Mobile = user.Mobile,
                DigiLockerId = user.DigiLockerId,
                // Set here too (not just in the "backfill" block below) -- that block only
                // fires when citizen.Mobile is still empty, but it's already populated by the
                // line above by the time it runs, so it would otherwise never stamp verification
                // for a brand-new citizen created via a verified source (DigiLocker/mobile-OTP).
                MobileVerifiedAt = user.MobileVerified ? now : null,
                AadhaarMasked = MaskAadhaar(user.Mobile),
                SevaId = BuildSevaId(user.Mobile),
                ProfileCompleted = false,
                CreatedAt = now,
                UpdatedAt = now
            };
            await _portal.InsertCitizenAsync(citizen, ct);
        }

        // Identity fact, not contact info -- once known it doesn't change, so only ever fill it
        // in when currently empty (e.g. a row created before this field existed).
        if (!string.IsNullOrWhiteSpace(user.DigiLockerId) && string.IsNullOrWhiteSpace(citizen.DigiLockerId))
        {
            citizen.DigiLockerId = user.DigiLockerId;
        }

        citizen.Username = string.IsNullOrWhiteSpace(user.Username) ? citizen.Username : user.Username;
        if (!string.IsNullOrWhiteSpace(user.Name) && !citizen.ProfileCompleted)
        {
            citizen.Name = user.Name;
        }

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            citizen.Email = user.Email;
        }

        // Mobile is citizen-owned contact info, not just an identity fact -- once a mobile is on
        // file, a later login (even a verified DigiLocker one, whose Aadhaar-linked number can
        // legitimately differ from the citizen's chosen SevaOne contact number) must NOT silently
        // overwrite it. Only auto-fill it the first time SevaOne has none on file at all.
        if (!string.IsNullOrWhiteSpace(user.Mobile) && string.IsNullOrWhiteSpace(citizen.Mobile))
        {
            citizen.Mobile = user.Mobile;
            citizen.SevaId = BuildSevaId(citizen.Mobile);
            // Only set for sources that actually proved ownership (DigiLocker's Aadhaar-linked
            // mobile, or SevaOne's own mobile+OTP flow) — never for WSO2's unverified userinfo
            // phone_number claim.
            if (user.MobileVerified)
            {
                citizen.MobileVerifiedAt = now;
            }
        }

        // Aadhaar/DOB/gender/address are DigiLocker-sourced identity facts, not editable contact
        // preferences -- once SevaOne has NO verified Aadhaar on file, a DigiLocker login should
        // always backfill it, even for a citizen who already completed registration via a weaker
        // path (WSO2/password) earlier. This only ever moves from unverified -> verified, never
        // overwrites an already-verified Aadhaar with a different one (that would mean a
        // different real person's Aadhaar landed on this account -- worth a loud warning, not a
        // silent overwrite).
        if (user.AadhaarVerified && !string.IsNullOrWhiteSpace(user.AadhaarHash))
        {
            if (string.IsNullOrWhiteSpace(citizen.AadhaarHash))
            {
                citizen.AadhaarMasked = user.AadhaarMasked;
                citizen.AadhaarHash = user.AadhaarHash;
                if (!string.IsNullOrWhiteSpace(user.Address)) citizen.Address = user.Address;
                if (!string.IsNullOrWhiteSpace(user.District)) citizen.District = user.District;
                if (!string.IsNullOrWhiteSpace(user.Taluk)) citizen.Taluk = user.Taluk;
                if (!string.IsNullOrWhiteSpace(user.StateName)) citizen.StateName = user.StateName;
                if (!string.IsNullOrWhiteSpace(user.VillageName)) citizen.VillageName = user.VillageName;
                if (!string.IsNullOrWhiteSpace(user.NameKannada)) citizen.NameKannada = user.NameKannada;
                if (!string.IsNullOrWhiteSpace(user.AddressKannada)) citizen.AddressKannada = user.AddressKannada;
                if (!string.IsNullOrWhiteSpace(user.DistrictKannada)) citizen.DistrictKannada = user.DistrictKannada;
                if (!string.IsNullOrWhiteSpace(user.StateKannada)) citizen.StateKannada = user.StateKannada;
                if (!string.IsNullOrWhiteSpace(user.VillageKannada)) citizen.VillageKannada = user.VillageKannada;
                if (user.Photo is not null) citizen.Photo = user.Photo;
            }
            else if (!string.Equals(citizen.AadhaarHash, user.AadhaarHash, StringComparison.Ordinal))
            {
                await _audit.InsertAsync(new AuditLog
                {
                    Id = Guid.NewGuid(),
                    CitizenId = citizen.Id,
                    Action = "auth.aadhaar_mismatch",
                    Resource = "citizen",
                    Detail = "DigiLocker login returned a DIFFERENT Aadhaar hash than the one already on file for this account — NOT auto-applied.",
                    IpAddress = _current.IpAddress,
                    At = now
                }, ct);
            }
        }

        if (user.DateOfBirth is { } dob && citizen.DateOfBirth is null)
        {
            citizen.DateOfBirth = dob;
        }

        if (!string.IsNullOrWhiteSpace(user.Gender) && !citizen.ProfileCompleted)
        {
            citizen.Gender = user.Gender;
        }

        // PoC-only: the seed script plants a demo "ananya" citizen with pre-populated demo
        // applications. The first real login as "ananya" absorbs that seed row's data and
        // applications into the real citizen, then removes the seed row.
        var seed = await _portal.GetCitizenByWso2SubjectAsync("demo-seed-ananya", ct);
        if (seed is not null && seed.Id != citizen.Id &&
            citizen.Username.Equals("ananya", StringComparison.OrdinalIgnoreCase))
        {
            await _orchestration.ReassignApplicationsAsync(seed.Id, citizen.Id, ct);

            citizen.Name = string.IsNullOrWhiteSpace(citizen.Name) ? seed.Name : citizen.Name;
            citizen.Mobile ??= seed.Mobile;
            citizen.Email ??= seed.Email ?? "ananya@seva.local";
            citizen.Address ??= seed.Address ?? "12, MG Road, Bengaluru";
            citizen.District ??= seed.District ?? "Bengaluru Urban";
            citizen.Taluk ??= seed.Taluk ?? "Bengaluru North";
            citizen.AadhaarMasked = seed.AadhaarMasked;
            citizen.SevaId = seed.SevaId;
            citizen.ProfileCompleted = true;
            await _portal.DeleteCitizenAsync(seed.Id, ct);
        }

        if (isNew && citizen.Username.Equals("ananya", StringComparison.OrdinalIgnoreCase) && !citizen.ProfileCompleted)
        {
            citizen.Name = string.IsNullOrWhiteSpace(citizen.Name) ? "Ananya Rao" : citizen.Name;
            citizen.Mobile ??= "9876543210";
            citizen.Email ??= "ananya@seva.local";
            citizen.Address ??= "12, MG Road, Bengaluru";
            citizen.District ??= "Bengaluru Urban";
            citizen.Taluk ??= "Bengaluru North";
            citizen.AadhaarMasked = MaskAadhaar(citizen.Mobile);
            citizen.SevaId = BuildSevaId(citizen.Mobile);
            citizen.ProfileCompleted = true;
        }

        citizen.ProfileFinishPct = ProfileCompleteness.Calculate(citizen);
        citizen.UpdatedAt = now;
        await _portal.UpdateCitizenAsync(citizen, ct);

        await _audit.InsertAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            CitizenId = citizen.Id,
            Action = "auth.login",
            Resource = "session",
            Detail = $"WSO2 subject {user.Subject}",
            IpAddress = _current.IpAddress,
            At = now
        }, ct);

        // WSO2 identity is now the citizen's actual login credential (see AuthController's
        // seva.wso2.at/rt cookies) — not a dormant portability side-effect anymore, so unlike
        // the rest of this codebase's WSO2 calls, this one is NOT soft-fail: if SevaOne can't
        // provision/exchange a real token, the citizen has no credential to receive and the
        // login itself must fail rather than silently degrade to something else.
        var wso2Tokens = await _wso2Identity.ProvisionAndExchangeAsync(
            citizen.SevaOneUserId, citizen.Name, citizen.Email, citizen.Mobile, ct)
            ?? throw new ForbiddenException("Unable to establish a secure session right now. Please try again in a moment.");

        var next = NormalizeReturnTo(returnTo);
        if (!citizen.ProfileCompleted)
        {
            next = "/register?next=" + Uri.EscapeDataString(next);
        }

        return (wso2Tokens, next, citizen.ProfileCompleted);
    }

    /// <summary>Casual sign-in / register lands on chat (/). Apply flows pass an explicit /handoff/... returnTo.</summary>
    public static string NormalizeReturnTo(string? returnTo)
    {
        if (string.IsNullOrWhiteSpace(returnTo) || !returnTo.StartsWith('/') || returnTo.StartsWith("//")
            || returnTo is "/" or "/dashboard")
        {
            return "/";
        }

        return returnTo;
    }

    private static string MaskAadhaar(string? mobile)
    {
        var last4 = string.IsNullOrWhiteSpace(mobile) || mobile.Length < 4 ? "0000" : mobile[^4..];
        return $"XXXX-XXXX-{last4}";
    }

    private static string BuildSevaId(string? mobile)
    {
        var last4 = string.IsNullOrWhiteSpace(mobile) || mobile.Length < 4 ? "0000" : mobile[^4..];
        return $"SO{last4}KA";
    }
}
