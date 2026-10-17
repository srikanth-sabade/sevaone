namespace SevaOne.Domain.Models;

public sealed class GovAdminActivityLog
{
    public Guid Id { get; set; }
    public string ActorUsername { get; set; } = "";
    public string ActorDisplayName { get; set; } = "";
    public string Action { get; set; } = "";
    public string? Target { get; set; }
    public string? Detail { get; set; }
    public DateTimeOffset At { get; set; }
}
