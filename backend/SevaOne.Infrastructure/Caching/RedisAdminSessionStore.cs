using System.Text.Json;
using StackExchange.Redis;

namespace SevaOne.Infrastructure.Caching;

public sealed class RedisAdminSessionStore : IAdminSessionStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly IConnectionMultiplexer _redis;

    public RedisAdminSessionStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task SaveAsync(AdminSession session, TimeSpan ttl, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync($"seva:adminsession:{session.SessionId}", JsonSerializer.Serialize(session, Json), ttl);
    }

    public async Task<AdminSession?> GetAsync(string sessionId, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync($"seva:adminsession:{sessionId}");
        return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize<AdminSession>((string)value!, Json);
    }

    public async Task DeleteAsync(string sessionId, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"seva:adminsession:{sessionId}");
    }
}
