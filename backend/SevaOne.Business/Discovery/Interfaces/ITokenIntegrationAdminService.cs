using SevaOne.Business.Discovery.DTOs;

namespace SevaOne.Business.Discovery.Interfaces;

/// <summary>Admin-side management of the token integration (see discovery.token_integration). Never
/// touches the operator-only Sakala fields (integration_method / sevasindhu_service_id).</summary>
public interface ITokenIntegrationAdminService
{
    Task<IReadOnlyList<TokenIntegrationTypeDto>> ListTypesAsync(CancellationToken ct);

    Task<IReadOnlyList<TokenIntegrationListItemDto>> ListServicesAsync(
        string? search, string? departmentCode, bool configuredOnly, bool citizenVisibleOnly, int limit, CancellationToken ct);

    Task<TokenIntegrationDto> GetAsync(string departmentCode, string serviceCode, CancellationToken ct);

    Task<TokenIntegrationDto> SaveAsync(string departmentCode, string serviceCode, TokenIntegrationWriteRequest request, string changedBy, CancellationToken ct);

    Task RemoveAsync(string departmentCode, string serviceCode, string changedBy, CancellationToken ct);

    // ---- settings shared by every service of a type (Kaveri single sign-on) ----
    Task<TokenProviderDto> GetProviderAsync(string typeKey, CancellationToken ct);

    Task<TokenProviderDto> SaveProviderAsync(string typeKey, TokenProviderWriteRequest request, string changedBy, CancellationToken ct);

    Task<TokenProviderTestResult> TestProviderLoginAsync(string typeKey, CancellationToken ct);

    // ---- logins departments use to call our acknowledgement API ----
    Task<IReadOnlyList<TokenIntegrationCredentialDto>> ListCredentialsAsync(string departmentCode, CancellationToken ct);

    /// <summary>Creates a login and returns its password once.</summary>
    Task<TokenIntegrationNewCredentialDto> CreateCredentialAsync(string departmentCode, string createdBy, CancellationToken ct);

    /// <summary>Sets a new password and returns it once.</summary>
    Task<TokenIntegrationNewCredentialDto> ResetCredentialAsync(string departmentCode, Guid credentialId, CancellationToken ct);

    Task SetCredentialActiveAsync(string departmentCode, Guid credentialId, bool isActive, CancellationToken ct);
}
