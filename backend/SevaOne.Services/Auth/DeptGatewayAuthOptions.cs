namespace SevaOne.Services.Auth;

/// <summary>Shared secret only WSO2 APIM's department gateway API injects (header modification policy on the backend-bound request) — proves a department-facing request actually came through the gateway, not straight to the backend port. Independent of DeptMachineAuth: this proves "APIM forwarded it," DeptMachineAuth proves "which department." Checked first, before DeptMachineAuth even runs.</summary>
public sealed class DeptGatewayAuthOptions
{
    public const string SectionName = "DeptGatewayAuth";

    public string Secret { get; set; } = string.Empty;
}
