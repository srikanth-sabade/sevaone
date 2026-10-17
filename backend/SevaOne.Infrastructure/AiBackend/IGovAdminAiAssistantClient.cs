namespace SevaOne.Infrastructure.AiBackend;

public interface IGovAdminAiAssistantClient
{
    Task<GovAdminAiAskResponse> AskAsync(string message, string sessionCookie, CancellationToken ct);
}

public sealed record GovAdminAiAskResponse(
    string Reply,
    IReadOnlyList<GovAdminAiSource> Sources,
    string ScopeNote);

public sealed record GovAdminAiSource(string Tool, string Description);
