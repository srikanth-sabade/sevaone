namespace SevaOne.Infrastructure.ExternalAuth;

/// <summary>
/// Carries a citizen's profile facts into CitizenSessionService.EstablishAsync. Despite the
/// name, no longer tied to WSO2 OIDC (that client was retired and deleted) -- DigiLocker and
/// mobile-OTP both build one of these locally from their own verified sources to keep
/// EstablishAsync as shared code across both login paths.
/// </summary>
public sealed class Wso2UserInfo
{
    public string Subject { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Mobile { get; set; }

    /// <summary>Only ever populated by the DigiLocker login path -- the CeG DigiLocker account id, unprefixed (Subject above carries the "digilocker:"-prefixed form for lookup).</summary>
    public string? DigiLockerId { get; set; }

    /// <summary>Only ever populated by the DigiLocker login path (WSO2 userinfo carries no DOB/gender claims).</summary>
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }

    /// <summary>True only when Mobile came from a source that actually proved the citizen owns that number (DigiLocker's Aadhaar-linked mobile, or SevaOne's own mobile+OTP flow). Drives Citizen.MobileVerifiedAt in CitizenSessionService.EstablishAsync.</summary>
    public bool MobileVerified { get; set; }

    /// <summary>True only when AadhaarMasked/AadhaarHash/address fields below came from CeG GetEAadharDetails. Drives whether CitizenSessionService.EstablishAsync trusts these fields over the registration form's manual entry.</summary>
    public bool AadhaarVerified { get; set; }
    public string? AadhaarMasked { get; set; }
    public string? AadhaarHash { get; set; }
    public string? Address { get; set; }
    public string? District { get; set; }
    public string? Taluk { get; set; }
    public string? StateName { get; set; }
    public string? VillageName { get; set; }

    /// <summary>Only ever populated by the DigiLocker login path -- name/address/district/state/
    /// village in the citizen's local language (Kannada), straight from eAadhaar's LData element.</summary>
    public string? NameKannada { get; set; }
    public string? AddressKannada { get; set; }
    public string? DistrictKannada { get; set; }
    public string? StateKannada { get; set; }
    public string? VillageKannada { get; set; }

    /// <summary>Only ever populated by the DigiLocker login path, from eAadhaar's Pht element.</summary>
    public byte[]? Photo { get; set; }
}
