using System.Text.Json;
using StackExchange.Redis;

namespace SevaOne.Infrastructure.Caching;

public sealed class RedisGovAdminSessionStore : IGovAdminSessionStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly IConnectionMultiplexer _redis;

    public RedisGovAdminSessionStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task SaveAsync(GovAdminSession session, TimeSpan ttl, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync($"seva:govadminsession:{session.SessionId}", JsonSerializer.Serialize(session, Json), ttl);
    }

    public async Task<GovAdminSession?> GetAsync(string sessionId, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync($"seva:govadminsession:{sessionId}");
        return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize<GovAdminSession>((string)value!, Json);
    }

    public async Task DeleteAsync(string sessionId, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"seva:govadminsession:{sessionId}");
    }

    public async Task<IReadOnlyList<GovAdminSession>> ListActiveSessionsAsync(Guid? forAdminId, CancellationToken ct)
    {
        var server = _redis.GetServers().FirstOrDefault(s => s.IsConnected);
        if (server is null)
        {
            return Array.Empty<GovAdminSession>();
        }

        var sessions = new List<GovAdminSession>();
        await foreach (var key in server.KeysAsync(pattern: "seva:govadminsession:*").WithCancellation(ct))
        {
            var value = await _redis.GetDatabase().StringGetAsync(key);
            if (value.IsNullOrEmpty)
            {
                continue;
            }

            var session = JsonSerializer.Deserialize<GovAdminSession>((string)value!, Json);
            if (session is not null && (!forAdminId.HasValue || session.GovAdminId == forAdminId.Value))
            {
                sessions.Add(session);
            }
        }

        return sessions.OrderByDescending(s => s.ExpiresAt).ToList();
    }
}
