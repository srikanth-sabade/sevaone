namespace SevaOne.Infrastructure.ExternalAuth;

public sealed class DigiLockerProfile
{
    /// <summary>CeG DigiLocker account id — stored as Wso2UserInfo.Subject / Citizen.Wso2Subject prefixed "digilocker:".</summary>
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
}

/// <summary>
/// The citizen's real Aadhaar number never leaves DigiLockerClient — this only ever carries the
/// masked display value and a salted hash (for dedupe/lookup). Address fields come from CeG
/// GetEAadharDetails (JSON and/or UIDAI XML). Any field that could not be parsed is left null.
/// </summary>
public sealed class DigiLockerAadhaarDetails
{
    public string AadhaarMasked { get; set; } = string.Empty;
    public string AadhaarHash { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? District { get; set; }
    public string? Taluk { get; set; }
    public string? StateName { get; set; }
    public string? VillageName { get; set; }
    /// <summary>Name/DOB/gender straight from the signed eAadhaar KYC record (Poi element) --
    /// this is the verified, authoritative source, stronger than the basic profile call
    /// (GetDigilockerIDFromTxnID), which often comes back sparse or empty for these fields.</summary>
    public string? Name { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }

    /// <summary>Name/address/district/state/village in the citizen's local language, from
    /// eAadhaar's LData element (lang="07" = Kannada on Karnataka's CeG gateway) -- same
    /// District/StateName/VillageName split as the English fields above, kept separate rather
    /// than folded into AddressKannada.</summary>
    public string? NameKannada { get; set; }
    public string? AddressKannada { get; set; }
    public string? DistrictKannada { get; set; }
    public string? StateKannada { get; set; }
    public string? VillageKannada { get; set; }

    /// <summary>Decoded JPEG bytes from eAadhaar's Pht element (base64 text content), when present
    /// and decodable. UNVERIFIED against a real CeG response as of this writing -- no DigiLocker
    /// test/sandbox mode exists (see backend/docs), so this has only been checked against the
    /// UIDAI eKYC XML spec, not a live payload. Soft-fails to null like every other field here.</summary>
    public byte[]? Photo { get; set; }
}

public sealed class DigiLockerIssuedDocument
{
    public string DocumentName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentUri { get; set; } = string.Empty;
    public string DocumentDescription { get; set; } = string.Empty;
    public string DocumentIssuer { get; set; } = string.Empty;
    public string DocumentUploadDate { get; set; } = string.Empty;
}

/// <summary>
/// Centre For e-Governance (Karnataka) DigiLocker Gateway. Citizen identity uses SignIn +
/// GetDigilockerIDFromTxnID; Aadhaar enrichment uses GetEAadharDetails; issued-doc listing uses
/// GetIssuedDocList. HMAC APIMac is Base64(HMAC-SHA256) over the concatenated fields CeG specifies.
/// </summary>
public interface IDigiLockerClient
{
    /// <summary>Calls DigiLockerSignInWithcodeChallange. Returns DigiLocker's browser RedirectURL.</summary>
    Task<string> StartSignInAsync(string appParam, string flow, CancellationToken ct);

    Task<DigiLockerProfile> GetProfileFromTxnAsync(string appParam, string txnId, CancellationToken ct);

    /// <summary>Soft-fails: returns null on any fetch/parse problem so login can still succeed without Aadhaar.</summary>
    Task<DigiLockerAadhaarDetails?> GetEAadhaarDetailsAsync(string digiLockerId, CancellationToken ct);

    Task<IReadOnlyList<DigiLockerIssuedDocument>> GetIssuedDocListAsync(string digiLockerId, CancellationToken ct);
}
