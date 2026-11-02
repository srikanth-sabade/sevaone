using Microsoft.AspNetCore.DataProtection;

namespace SevaOne.Infrastructure.Security;

/// <summary>Encrypts the department-session payload stored in orchestration.department_sessions.encrypted_payload.</summary>
public interface IPayloadProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedPayload);
}

public sealed class DataProtectionPayloadProtector : IPayloadProtector
{
    private readonly IDataProtector _protector;

    public DataProtectionPayloadProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("SevaOne.DepartmentSession.v1");
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string protectedPayload) => _protector.Unprotect(protectedPayload);
}
