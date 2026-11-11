using SevaOne.Domain.Common;

namespace SevaOne.Domain.Models;

/// <summary>
/// Citizen / SevaOne user. <see cref="BaseModel.Id"/> stays the internal surrogate key (all
/// FKs in this schema reference it). <see cref="SevaOneUserId"/> is the separate, sequential,
/// externally-facing "SevaOneUserID" the user flow passes to departments in Apply URLs and JWS
/// claims — dual-key pattern: UUID for internal referential integrity, BIGINT for a stable,
/// compact, human/URL-friendly cross-system identifier.
/// </summary>
public sealed class Citizen : BaseModel
{
    /// <summary>Sequential public identifier — "SevaOneUserID" in the user flow. DB-generated (IDENTITY).</summary>
    public long SevaOneUserId { get; set; }

    public string Wso2Subject { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Nullable: WSO2 userinfo doesn't always carry a phone claim, so a freshly-provisioned
    /// citizen can transiently have none. The "mobile is mandatory" rule is enforced at the
    /// registration/apply API boundary, not here — see <see cref="MobileVerifiedAt"/>.
    /// </summary>
    public string? Mobile { get; set; }

    /// <summary>Set once Mobile has passed OTP verification. Null = unverified/never verified.</summary>
    public DateTimeOffset? MobileVerifiedAt { get; set; }

    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? District { get; set; }
    public string? Taluk { get; set; }
    public string? StateName { get; set; }
    public string? VillageName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }

    /// <summary>Occupation category code (see OccupationCategories); drives service recommendations.</summary>
    public string? Occupation { get; set; }

    /// <summary>Name/address/district/state/village in the citizen's local language (Kannada),
    /// straight from eAadhaar's LData element -- only ever populated via DigiLocker login. Same
    /// District/StateName/VillageName split as the English fields above.</summary>
    public string? NameKannada { get; set; }
    public string? AddressKannada { get; set; }
    public string? DistrictKannada { get; set; }
    public string? StateKannada { get; set; }
    public string? VillageKannada { get; set; }

    /// <summary>Masked Aadhaar for display (e.g. "XXXX-XXXX-3210"). Never the raw number.</summary>
    public string? AadhaarMasked { get; set; }

    /// <summary>Salted hash of the Aadhaar number for dedupe/lookup — never the raw number.</summary>
    public string? AadhaarHash { get; set; }

    /// <summary>CeG DigiLocker account id, unprefixed — set only by the DigiLocker login path. Wso2Subject carries the "digilocker:"-prefixed form used for lookup; this is the same value on its own for anything that needs it directly.</summary>
    public string? DigiLockerId { get; set; }

    /// <summary>JPEG bytes from eAadhaar's Pht element, when DigiLocker returned one and it decoded. Null for citizens who registered via mobile OTP only, or whose eAadhaar response had none.</summary>
    public byte[]? Photo { get; set; }

    public string SevaId { get; set; } = string.Empty;
    public bool ProfileCompleted { get; set; }

    /// <summary>0-100 profile-completion percentage shown to the citizen post-login. Authoritative over the legacy <see cref="ProfileCompleted"/> flag.</summary>
    public int ProfileFinishPct { get; set; }

    /// <summary>Operator business id (OperatorMaster.OperatorCode) when created via Assisted Mode; null for self-service registration.</summary>
    public string? CreatedByOperatorId { get; set; }

    /// <summary>Karnataka Government ID. Set only by backend ingestion when provisioning a government official ahead of their first login -- never editable via any UI. Null for an ordinary citizen.</summary>
    public string? Kgid { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
