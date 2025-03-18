using System.Text;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;

using NATS.Client.Core;

namespace Stebet.Nats.DistributedCache.Benchmarks;

[MemoryDiagnoser]
public class NatsBenchmarks
{
    private readonly NatsDistributedCache _natsDistributedCache = new(new NatsDistributedCacheOptions(), new NatsConnection(new NatsOpts()
    {
        Url = "nats://localhost:4222"
    }));

    private readonly RedisCache _redisCache = new(new RedisCacheOptions()
    {
        ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions()
        {
            EndPoints = { "localhost:6379" },
            Password = "blabla"
        }
    });

    private readonly DistributedCacheEntryOptions _distributedCacheEntryOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
    private readonly byte[] _data = Encoding.UTF8.GetBytes("Hello, World!");

    public NatsBenchmarks()
    {
        _natsDistributedCache.Set("AlreadySet", _data, _distributedCacheEntryOptions);
        _redisCache.Set("AlreadySet", _data, _distributedCacheEntryOptions);
    }

    [Benchmark]
    [ArgumentsSource(nameof(Caches))]
    public async Task TestPutWithStaticKeyAsync(IDistributedCache cache)
    {
        await cache.SetAsync(nameof(TestPutWithStaticKeyAsync), _data, _distributedCacheEntryOptions).ConfigureAwait(false);
    }

    [Benchmark]
    [ArgumentsSource(nameof(Caches))]
    public async Task TestPutWithRandomKeyAsync(IDistributedCache cache)
    {
        await cache.SetAsync(Guid.NewGuid().ToString(), _data, _distributedCacheEntryOptions).ConfigureAwait(false);
    }

    [Benchmark]
    [ArgumentsSource(nameof(Caches))]
    public async Task<byte[]?> TestGetAsync(IDistributedCache cache)
    {
        return await cache.GetAsync("AlreadySet").ConfigureAwait(false);
    }

    public IEnumerable<IDistributedCache> Caches()
    {
        yield return _natsDistributedCache;
        yield return _redisCache;
    }
}
