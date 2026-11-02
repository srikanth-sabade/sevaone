using SevaOne.Business.Discovery.DTOs;

namespace SevaOne.Business.Discovery.Interfaces;

/// <summary>The department-facing token check: "is this the token SevaOne issued for this application, and who is the user?"</summary>
public interface ITokenVerificationService
{
    Task<TokenVerifyResult> VerifyAsync(TokenVerifyRequest request, TokenIntegrationCaller caller, CancellationToken ct);
}

/// <summary>What a department can do to try its integration safely (Department Guide): practice applications that never reach a
/// citizen, and test calls to its own landing page and status API.</summary>
public interface IDeptPortalService
{
    Task<SandboxStartDto> StartSandboxAsync(TokenIntegrationCaller caller, string? serviceCode, CancellationToken ct);
    Task<TokenVerifyResult> VerifySandboxAsync(TokenVerifyRequest request, TokenIntegrationCaller caller, CancellationToken ct);
    Task<(TokenAckOutcome Outcome, bool NotSandbox)> AcknowledgeSandboxAsync(TokenAckRequest request, TokenIntegrationCaller caller, string? callerIp, CancellationToken ct);
    Task<LandingTestResult> TestLandingAsync(TokenIntegrationCaller caller, string url, string? serviceCode, CancellationToken ct);
    Task<TokenStatusTestResult> TestStatusAsync(StatusTestRequest request, CancellationToken ct);
}
