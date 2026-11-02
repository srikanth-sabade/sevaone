using System.Text.Json;
using StackExchange.Redis;

namespace SevaOne.Infrastructure.Caching;

public sealed class RedisSevaSessionStore : ISevaSessionStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly IConnectionMultiplexer _redis;

    public RedisSevaSessionStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task SaveLoginStateAsync(string state, LoginState payload, TimeSpan ttl, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync($"seva:login:{state}", JsonSerializer.Serialize(payload, Json), ttl);
    }

    private const string GetDeleteScript = "local v = redis.call('GET', KEYS[1]) if v then redis.call('DEL', KEYS[1]) end return v";

    public async Task<LoginState?> TakeLoginStateAsync(string state, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var key = $"seva:login:{state}";
        // Atomic get-and-delete via a Lua script (EVAL, supported since Redis 2.6) rather than
        // GETDEL (needs Redis 6.2+, not available on every deployment target here) -- a plain
        // GET then DEL isn't one-time-use under concurrent requests: two near-simultaneous
        // callbacks with the same state could both read before either deletes, each minting its
        // own session from the same login state.
        var value = (RedisValue)await db.ScriptEvaluateAsync(GetDeleteScript, [key]);
        if (value.IsNullOrEmpty)
        {
            return null;
        }

        return JsonSerializer.Deserialize<LoginState>((string)value!, Json);
    }
}
