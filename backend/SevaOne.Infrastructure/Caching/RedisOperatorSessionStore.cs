using System.Text.Json;
using StackExchange.Redis;

namespace SevaOne.Infrastructure.Caching;

public sealed class RedisOperatorSessionStore : IOperatorSessionStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly IConnectionMultiplexer _redis;

    public RedisOperatorSessionStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task SaveAsync(OperatorSession session, TimeSpan ttl, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync($"seva:opsession:{session.SessionId}", JsonSerializer.Serialize(session, Json), ttl);
    }

    public async Task<OperatorSession?> GetAsync(string sessionId, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync($"seva:opsession:{sessionId}");
        return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize<OperatorSession>((string)value!, Json);
    }

    public async Task DeleteAsync(string sessionId, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"seva:opsession:{sessionId}");
    }
}
