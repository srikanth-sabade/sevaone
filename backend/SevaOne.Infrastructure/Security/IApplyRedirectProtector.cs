using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace SevaOne.Infrastructure.Security;

/// <summary>
/// Encrypts the JWS apply-token before it goes into the redirect URL's query string
/// (Token=...), and decrypts it back on the way in via DeptTokenGate.ResolveAsync.
///
/// This exists because the JWS itself is only *signed*, not confidential -- its claims
/// (SevaOneUserId, DepartmentId, ServiceId, etc.) are base64-readable by anyone who sees
/// the redirect URL. Some department integration specs (e.g. the eCourt Services doc)
/// mandate encrypting the redirect payload itself, separately from the token's own
/// tamper-evidence, with "the specific algorithm, key, and code... provided by [the
/// department's integrator]".
///
/// Interim choice: ASP.NET Core Data Protection (AES under the hood), the same mechanism
/// already used for orchestration.department_sessions.encrypted_payload (see
/// DataProtectionPayloadProtector) -- production-grade, key-rotation-capable, and already
/// wired into this app's DI/Redis key ring. This works because the department never needs
/// to decrypt the value itself: it only ever relays the opaque string back to SevaOne's own
/// /api/v1/dept/* endpoints, exactly like it relays the plain JWS today. If a specific
/// department integration mandates a *different* algorithm/key that THEY decrypt
/// themselves, that department's flow needs its own protector implementation instead of
/// this one -- swap it in via DI for that department's connector, don't change this default.
/// A distinct "SevaOne.ApplyTokenRedirect.v1" purpose isolates this key ring from the
/// department-session one, so a compromise of one can't be replayed against the other.
/// </summary>
public interface IApplyRedirectProtector
{
    string Protect(string plaintext);

    /// <summary>Returns null on any decrypt failure (corrupted/foreign payload) rather than throwing -- callers treat this the same as any other "token invalid" case.</summary>
    string? TryUnprotect(string protectedPayload);
}

public sealed class DataProtectionApplyRedirectProtector : IApplyRedirectProtector
{
    private readonly IDataProtector _protector;

    public DataProtectionApplyRedirectProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("SevaOne.ApplyTokenRedirect.v1");
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string? TryUnprotect(string protectedPayload)
    {
        try
        {
            return _protector.Unprotect(protectedPayload);
        }
        catch (CryptographicException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
