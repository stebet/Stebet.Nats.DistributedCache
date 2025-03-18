using System.Runtime.CompilerServices;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

using NATS.Client.Core;
using NATS.Client.KeyValueStore;
using NATS.Net;

namespace Stebet.Nats.DistributedCache;

/// <summary>
/// Implementation of <see cref="IDistributedCache"/> that uses NATS KeyValue Store to store the cache. Requires NATS Server Version 2.11 or higher.
/// </summary>
public class NatsDistributedCache : IDistributedCache
{
    private readonly INatsKVContext _kvContext;
    private readonly INatsKVStore _natsKvStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="NatsDistributedCache"/> class.
    /// </summary>
    /// <param name="options">The options for the cache.</param>
    /// <param name="connection">The NATS connection to use.</param>
    public NatsDistributedCache(IOptions<NatsDistributedCacheOptions> options, INatsConnection connection)
    {
        _kvContext = connection.CreateJetStreamContext().CreateKeyValueStoreContext();
        var createOrUpdateTask = _kvContext.CreateOrUpdateStoreAsync(new NatsKVConfig(options.Value.BucketName)
        {
            AllowMsgTTL = true
        }).AsTask();
        createOrUpdateTask.Wait();
        _natsKvStore = createOrUpdateTask.Result;
    }

    /// <inheritdoc/>
    public byte[]? Get(string key)
    {
        var result = GetAsync(key);
        result.Wait();
        return result.Result;
    }

    /// <inheritdoc/>
    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        var result = await _natsKvStore.TryGetEntryAsync<byte[]>(key, cancellationToken: token).ConfigureAwait(false);
        if (result.Success)
        {
            return result.Value.Value;
        }

        return null;
    }

    /// <inheritdoc/>
    public void Refresh(string key)
    {
        return;
    }

    /// <inheritdoc/>
    public Task RefreshAsync(string key, CancellationToken token = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Remove(string key)
    {
        var result = RemoveAsync(key);
        result.Wait();
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        await _natsKvStore.DeleteAsync(key, cancellationToken: token).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        var result = SetAsync(key, value, options);
        result.Wait();
    }

    /// <inheritdoc/>
    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var ttl = ValidateCacheOptionsAndDetermineTtl(options);

        return ttl.HasValue
            ? _natsKvStore.PutAsync(key, value, ttl.Value, cancellationToken: token).AsTask()
            : _natsKvStore.PutAsync(key, value, cancellationToken: token).AsTask();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TimeSpan? ValidateCacheOptionsAndDetermineTtl(DistributedCacheEntryOptions options)
    {
        if (options.SlidingExpiration.HasValue)
        {
            return ThrowSlidingExpirationNotSupportedException();
        }
        else if (options.AbsoluteExpirationRelativeToNow.HasValue)
        {
            return options.AbsoluteExpirationRelativeToNow.Value;
        }
        else
        {
            return options.AbsoluteExpiration.HasValue ? (options.AbsoluteExpiration.Value - DateTimeOffset.UtcNow) : null;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static TimeSpan? ThrowSlidingExpirationNotSupportedException()
    {
        throw new NotSupportedException("Sliding expiration is not supported by this cache implementation yet...");
    }
}
