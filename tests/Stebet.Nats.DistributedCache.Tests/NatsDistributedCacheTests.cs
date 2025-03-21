using System.Buffers;

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
        _cache.Set(key, guidBytes, new DistributedCacheEntryOptions() { AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(2) });
        var result = _cache.Get(key);
        Assert.Equal(guidBytes, result);
        await Task.Delay(3000);
        result = _cache.Get(key);
        Assert.Null(result);
    }

    [Fact]
    public async Task TestAbsoluteExpirationAsync()
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
    public async Task TestAbsoluteExpirationBufferedAsync()
    {
        var guid = Guid.NewGuid();
        var guidBytes = new ReadOnlySequence<byte>(guid.ToByteArray());
        ArrayBufferWriter<byte> buffer = new();
        var key = guid.ToString();
        await _cache.SetAsync(key, guidBytes, new DistributedCacheEntryOptions() { AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(2) });
        var result = await _cache.TryGetAsync(key, buffer);
        Assert.True(result);
        Assert.Equal(guidBytes.ToArray(), buffer.WrittenSpan.ToArray());
        await Task.Delay(3000);
        result = await _cache.TryGetAsync(key, buffer);
        Assert.False(result);
    }

    [Fact]
    public async Task TestAbsoluteExpirationBuffered()
    {
        var guid = Guid.NewGuid();
        var guidBytes = new ReadOnlySequence<byte>(guid.ToByteArray());
        ArrayBufferWriter<byte> buffer = new();
        var key = guid.ToString();
        _cache.Set(key, guidBytes, new DistributedCacheEntryOptions() { AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(2) });
        var result = _cache.TryGet(key, buffer);
        Assert.True(result);
        Assert.Equal(guidBytes.ToArray(), buffer.WrittenSpan.ToArray());
        await Task.Delay(3000);
        result = _cache.TryGet(key, buffer);
        Assert.False(result);
    }

    [Fact]
    public async Task TestAbsoluteExpirationRelativeToNow()
    {
        var guid = Guid.NewGuid();
        var guidBytes = guid.ToByteArray();
        var key = guid.ToString();
        _cache.Set(key, guidBytes, new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2) });
        var result = _cache.Get(key);
        Assert.Equal(guidBytes, result);
        await Task.Delay(3000);
        result = _cache.Get(key);
        Assert.Null(result);
    }

    [Fact]
    public async Task TestAbsoluteExpirationRelativeToNowAsync()
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

    [Fact]
    public async Task TestAbsoluteExpirationRelativeToNowBuffered()
    {
        var guid = Guid.NewGuid();
        var guidBytes = new ReadOnlySequence<byte>(guid.ToByteArray());
        ArrayBufferWriter<byte> buffer = new();
        var key = guid.ToString();
        _cache.Set(key, guidBytes, new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2) });
        var result = _cache.TryGet(key, buffer);
        Assert.True(result);
        Assert.Equal(guidBytes.ToArray(), buffer.WrittenSpan.ToArray());
        await Task.Delay(3000);
        result = _cache.TryGet(key, buffer);
        Assert.False(result);
    }

    [Fact]
    public async Task TestAbsoluteExpirationRelativeToNowBufferedAsync()
    {
        var guid = Guid.NewGuid();
        var guidBytes = new ReadOnlySequence<byte>(guid.ToByteArray());
        ArrayBufferWriter<byte> buffer = new();
        var key = guid.ToString();
        await _cache.SetAsync(key, guidBytes, new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2) });
        var result = await _cache.TryGetAsync(key, buffer);
        Assert.True(result);
        Assert.Equal(guidBytes.ToArray(), buffer.WrittenSpan.ToArray());
        await Task.Delay(3000);
        result = await _cache.TryGetAsync(key, buffer);
        Assert.False(result);
    }

    [Fact]
    public void TestRemove()
    {
        var guid = Guid.NewGuid();
        var guidBytes = guid.ToByteArray();
        var key = guid.ToString();
        _cache.Set(key, guidBytes, new DistributedCacheEntryOptions());
        var result = _cache.Get(key);
        Assert.Equal(guidBytes, result);
        _cache.Remove(key);
        result = _cache.Get(key);
        Assert.Null(result);
    }

    [Fact]
    public async Task TestRemoveAsync()
    {
        var guid = Guid.NewGuid();
        var guidBytes = guid.ToByteArray();
        var key = guid.ToString();
        await _cache.SetAsync(key, guidBytes, new DistributedCacheEntryOptions());
        var result = await _cache.GetAsync(key);
        Assert.Equal(guidBytes, result);
        await _cache.RemoveAsync(key);
        result = await _cache.GetAsync(key);
        Assert.Null(result);
    }

    [Fact]
    public async Task SlidingExpirationNotSupported()
    {
        var guid = Guid.NewGuid();
        var guidBytes = guid.ToByteArray();
        var key = guid.ToString();
        var exception = await Assert.ThrowsAsync<NotSupportedException>(async () => await _cache.SetAsync(key, guidBytes, new DistributedCacheEntryOptions() { SlidingExpiration = TimeSpan.FromSeconds(2) }).ConfigureAwait(false));
        Assert.Equal("Sliding expiration is not supported by this cache implementation yet...", exception.Message);
    }
}
