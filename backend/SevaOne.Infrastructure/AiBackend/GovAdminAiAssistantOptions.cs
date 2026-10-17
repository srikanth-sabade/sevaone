namespace SevaOne.Infrastructure.AiBackend;

public sealed class GovAdminAiAssistantOptions
{
    public const string SectionName = "GovAdminAiAssistant";

    public string BaseUrl { get; set; } = "http://gov-admin-ai:8090";
}
