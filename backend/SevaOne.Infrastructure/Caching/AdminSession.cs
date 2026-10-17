namespace SevaOne.Infrastructure.Caching;

/// <summary>The catalog admin's cookie session, cached in Redis -- mirrors OperatorSession. Separate cookie, separate auth scheme -- an admin, an operator, and a citizen can all be logged in on the same browser at once.</summary>
public sealed class AdminSession
{
    public string SessionId { get; set; } = string.Empty;
    public Guid AdminId { get; set; }
    public string AdminCode { get; set; } = string.Empty;
    public string? AdminName { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
