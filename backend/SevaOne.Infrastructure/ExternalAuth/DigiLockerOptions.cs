namespace SevaOne.Infrastructure.ExternalAuth;

public sealed class DigiLockerOptions
{
    public const string SectionName = "DigiLocker";

    /// <summary>CeG DigiLocker Gateway service root (no trailing slash). Fill DeptKey/HmacKey before calling live APIs.</summary>
    public string BaseUrl { get; set; } = "http://103.138.197.55/dlapi/API/CeGLockerAPI.svc";

    /// <summary>Department key issued by CeG. Leave empty until onboarded; start/callback will fail closed.</summary>
    public string DeptKey { get; set; } = string.Empty;

    /// <summary>HMAC secret for APIMac. If empty, <see cref="DeptKey"/> is used as the HMAC key. Store via user-secrets, never commit a real value.</summary>
    public string HmacKey { get; set; } = string.Empty;

    /// <summary>SevaOne URL CeG/DigiLocker redirects the browser to after sign-in. Must match the URL registered with CeG.</summary>
    public string CallbackUrl { get; set; } = "http://localhost:5080/api/v1/auth/digilocker/callback";

    /// <summary>HMAC key used to hash the citizen's real Aadhaar number before it's stored (Citizen.AadhaarHash) — the raw number itself is never persisted. Dev default only — every real environment must override this via secret/config.</summary>
    public string AadhaarHashSalt { get; set; } = "dev-only-aadhaar-hash-salt-change-in-every-real-environment";
}
