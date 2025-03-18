using Microsoft.Extensions.Caching.Distributed;

using NATS.Client.Core;

namespace Stebet.Nats.DistributedCache.Tests;

public class NatsDistributedCacheTests
{
    private readonly NatsConnection _connection;
    private readonly NatsDistributedCache _cache;

    public NatsDistributedCacheTests()
    {
        _connection = new NatsConnection(new NatsOpts()
        {
            Url = "nats://localhost:4222"
        });
        _cache = new NatsDistributedCache(new NatsDistributedCacheOptions(), _connection);
    }

    [Fact]
    public async Task TestAbsoluteExpiration()
    {
        var guid = Guid.NewGuid();
        var guidBytes = guid.ToByteArray();
        var key = guid.ToString();
        await _cache.SetAsync(key, guidBytes, new DistributedCacheEntryOptions() { AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(2) });
        var result = await _cache.GetAsync(key);
        Assert.Equal(guidBytes, result);
        await Task.Delay(3000);
        result = await _cache.GetAsync(key);
        Assert.Null(result);
    }

    [Fact]
    public async Task TestAbsoluteExpirationRelativeToNow()
    {
        var guid = Guid.NewGuid();
        var guidBytes = guid.ToByteArray();
        var key = guid.ToString();
        await _cache.SetAsync(key, guidBytes, new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2) });
        var result = await _cache.GetAsync(key);
        Assert.Equal(guidBytes, result);
        await Task.Delay(3000);
        result = await _cache.GetAsync(key);
        Assert.Null(result);
    }
}
