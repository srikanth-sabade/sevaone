using SevaOne.Business.Discovery.DTOs;

namespace SevaOne.Business.Discovery.Interfaces;

/// <summary>The department-facing acknowledgement API of the token integration (Seva Sindhu V3, URL 2).</summary>
public interface ITokenAcknowledgementService
{
    /// <summary>Checks a Basic Authorization header against the logins we issued. Null when missing or wrong.</summary>
    Task<TokenIntegrationCaller?> AuthenticateAsync(string? authorizationHeader, CancellationToken ct);

    /// <summary>The signed-in citizen's own acknowledgement document, or null when there is none.</summary>
    Task<TokenIntegrationFileDto?> GetAcknowledgementFileAsync(string reference, CancellationToken ct);

    Task<TokenAckOutcome> AcknowledgeAsync(TokenAckRequest request, TokenIntegrationCaller caller, string? callerIp, CancellationToken ct);
}
