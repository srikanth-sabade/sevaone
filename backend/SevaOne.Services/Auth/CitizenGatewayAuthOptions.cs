namespace SevaOne.Services.Auth;

/// <summary>Shared secret only WSO2 APIM's citizen gateway API injects (header modification policy on the backend-bound request) — proves a citizen-facing request actually came through the gateway, not straight to the backend port. Mirrors DeptMachineAuth's role for department traffic, but a single fixed secret rather than a per-department lookup table, since there's one gateway, not many callers to distinguish.</summary>
public sealed class CitizenGatewayAuthOptions
{
    public const string SectionName = "CitizenGatewayAuth";

    public string Secret { get; set; } = string.Empty;
}
