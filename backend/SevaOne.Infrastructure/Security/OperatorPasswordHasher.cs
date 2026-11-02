using System.Security.Cryptography;

namespace SevaOne.Infrastructure.Security;

/// <summary>
/// Operators have no WSO2 IS account (citizens do, via OIDC/password-grant) -- there's no
/// external IdP for internal government-staff/kiosk accounts, so SevaOne owns this hash
/// itself. PBKDF2-SHA256 via the BCL, no extra package, matching this codebase's existing
/// preference for hand-rolled crypto over pulling in Microsoft.AspNetCore.Identity for one
/// hash/verify pair (compare HashOtp in PortalAuthService.cs).
/// Encoded as "{iterations}.{saltBase64}.{hashBase64}" -- self-describing so the iteration
/// count can be raised later without invalidating already-hashed passwords.
/// </summary>
public static class OperatorPasswordHasher
{
    private const int Iterations = 210_000; // OWASP 2023+ minimum recommendation for PBKDF2-SHA256
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string? encoded)
    {
        if (string.IsNullOrWhiteSpace(encoded))
        {
            return false;
        }

        var parts = encoded.Split('.', 3);
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
        {
            return false;
        }

        byte[] salt, expected;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expected = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
