using StackExchange.Redis;

namespace SevaOne.Infrastructure.Caching;

/// <summary>A small shared store for the access and refresh tokens SevaOne holds for an outside system (Kaveri), so every
/// running copy of the backend sees the same tokens. Values handed in are already encrypted.</summary>
public interface ITokenCache
{
    Task<string?> GetAsync(string key, CancellationToken ct);
    Task SetAsync(string key, string value, TimeSpan lifetime, CancellationToken ct);
    Task RemoveAsync(string key, CancellationToken ct);

    /// <summary>Takes a short lock so only one caller at a time renews tokens (a refresh token works once). Null when
    /// someone else holds it. Dispose to release.</summary>
    Task<IAsyncDisposable?> TryLockAsync(string key, TimeSpan lifetime, CancellationToken ct);
}

public sealed class RedisTokenCache : ITokenCache
{
    private const string Prefix = "seva:tokencache:";
    private readonly IConnectionMultiplexer _redis;

    public RedisTokenCache(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<string?> GetAsync(string key, CancellationToken ct)
    {
        var value = await _redis.GetDatabase().StringGetAsync(Prefix + key);
        return value.IsNullOrEmpty ? null : (string?)value;
    }

    public Task SetAsync(string key, string value, TimeSpan lifetime, CancellationToken ct) =>
        _redis.GetDatabase().StringSetAsync(Prefix + key, value, lifetime);

    public Task RemoveAsync(string key, CancellationToken ct) => _redis.GetDatabase().KeyDeleteAsync(Prefix + key);

    public async Task<IAsyncDisposable?> TryLockAsync(string key, TimeSpan lifetime, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var token = Guid.NewGuid().ToString("N");
        var redisKey = Prefix + "lock:" + key;
        if (!await db.StringSetAsync(redisKey, token, lifetime, When.NotExists)) return null;
        return new Release(db, redisKey, token);
    }

    private sealed class Release : IAsyncDisposable
    {
        private readonly IDatabase _db;
        private readonly string _key;
        private readonly string _token;

        public Release(IDatabase db, string key, string token)
        {
            _db = db;
            _key = key;
            _token = token;
        }

        // Only the holder may release: delete the key only if it still carries this holder's token.
        public async ValueTask DisposeAsync() =>
            await _db.ScriptEvaluateAsync("if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end",
                [(RedisKey)_key], [(RedisValue)_token]);
    }
}
