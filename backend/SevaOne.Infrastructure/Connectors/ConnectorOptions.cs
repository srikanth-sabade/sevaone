namespace SevaOne.Infrastructure.Connectors;

public sealed class ConnectorOptions
{
    public const string SectionName = "Connectors";

    public string HubBaseUrl { get; set; } = "http://localhost:5092";
    public int DepartmentSessionMinutes { get; set; } = 15;
}
