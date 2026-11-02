namespace SevaOne.Infrastructure.Security;

public sealed class JwsOptions
{
    public const string SectionName = "Jws";

    public string Issuer { get; set; } = "sevaone";

    /// <summary>HMAC-SHA256 signing key. Dev default only — every real environment must override this via secret/config, never commit a real value.</summary>
    public string SigningKey { get; set; } = "dev-only-signing-key-change-in-every-real-environment-min-32-bytes";

    /// <summary>Normal "Token Validate" session rule: 30 minutes.</summary>
    public int SessionMinutes { get; set; } = 30;
}
