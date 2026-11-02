using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace SevaOne.Infrastructure.Security;

/// <summary>Encrypts the secret an admin enters for a department's status API (token integration URL 3),
/// so it is never stored or logged in clear. It can be decrypted only to make the call; no screen reads it back.</summary>
public interface ITokenStatusSecretProtector
{
    string Protect(string plaintext);
    string? TryUnprotect(string protectedPayload);
}

public sealed class DataProtectionTokenStatusSecretProtector : ITokenStatusSecretProtector
{
    private readonly IDataProtector _protector;

    public DataProtectionTokenStatusSecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("SevaOne.TokenIntegration.StatusSecret.v1");
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string? TryUnprotect(string protectedPayload)
    {
        try { return _protector.Unprotect(protectedPayload); }
        catch (CryptographicException) { return null; }
        catch (FormatException) { return null; }
    }
}
