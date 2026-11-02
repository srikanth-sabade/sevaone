namespace SevaOne.Domain.Models;

/// <summary>Operator login only -- never returned to dept-facing code paths (see portal.sp_get_operator_credentials).</summary>
public sealed class OperatorCredentials
{
    public Guid Id { get; set; }
    public string OperatorCode { get; set; } = string.Empty;
    public string? OperatorName { get; set; }
    public string? MobileNo { get; set; }
    public string? PasswordHash { get; set; }

    /// <summary>Non-null means this operator authenticates against WSO2 IS (see
    /// OperatorAuthService.LoginAsync); PasswordHash is only consulted when this is null.</summary>
    public string? Wso2Subject { get; set; }

    public bool IsActive { get; set; }
}
