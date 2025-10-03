using System.Buffers;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

using NATS.Client.Core;
using NATS.Client.KeyValueStore;
using NATS.Net;

namespace Stebet.Nats.DistributedCache;

/// <summary>
/// Implementation of <see cref="IBufferDistributedCache"/> that uses NATS KeyValue Store to store the cache. Requires NATS Server Version 2.11 or higher.
/// </summary>
public class NatsDistributedCache : IBufferDistributedCache
{
    private readonly INatsKVContext _kvContext;
    private INatsKVStore _natsKvStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="NatsDistributedCache"/> class.
    /// </summary>
    /// <param name="options">The options for the cache.</param>
    /// <param name="connection">The NATS connection to use.</param>
    public NatsDistributedCache(IOptions<NatsDistributedCacheOptions> options, INatsConnection connection)
    {
        var _streamContext = connection.CreateJetStreamContext();
        _kvContext = _streamContext.CreateKeyValueStoreContext();
        var createOrUpdateTask = Task.Run(async () =>
        {
            _natsKvStore = await _kvContext.CreateOrUpdateStoreAsync(new NatsKVConfig(options.Value.BucketName) {  LimitMarkerTTL = TimeSpan.FromMinutes(10)}).ConfigureAwait(false);
            //await _natsKvStore.JetStreamContext.UpdateStreamAsync(new NATS.Client.JetStream.Models.StreamConfig(_natsKvStore.) { AllowMsgTTL = true }).ConfigureAwait(false);
        });

        createOrUpdateTask.Wait();
        if (_natsKvStore == null)
        {
            throw new InvalidOperationException("Failed to create or update the NATS KeyValue Store.");
        }
    }

    /// <inheritdoc/>
    public byte[]? Get(string key) => GetAsync(key).GetAwaiter().GetResult();

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
    public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

    /// <inheritdoc/>
    public void Remove(string key) => RemoveAsync(key).Wait();

    /// <inheritdoc/>
    public async Task RemoveAsync(string key, CancellationToken token = default) => await _natsKvStore.DeleteAsync(key, cancellationToken: token).ConfigureAwait(false);

    /// <inheritdoc/>
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => SetAsync(key, value, options).Wait();

    /// <inheritdoc/>
    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(options);
#else
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }
#endif

        var ttl = ValidateCacheOptionsAndDetermineTtl(options);

        return ttl.HasValue
            ? _natsKvStore.CreateAsync(key, value, ttl.Value, cancellationToken: token).AsTask()
            : _natsKvStore.CreateAsync(key, value, cancellationToken: token).AsTask();
    }

    public bool TryGet(string key, IBufferWriter<byte> destination) => TryGetAsync(key, destination).AsTask().GetAwaiter().GetResult();

    /// <inheritdoc/>
    public async ValueTask<bool> TryGetAsync(string key, IBufferWriter<byte> destination, CancellationToken token = default)
    {
        var result = await _natsKvStore.TryGetEntryAsync<NatsMemoryOwner<byte>>(key, cancellationToken: token).ConfigureAwait(false);
        if (result.Success)
        {
            using var memoryOwner = result.Value.Value;
            destination.Write(memoryOwner.Memory.Span);
            return true;
        }

        return false;
    }

    public void Set(string key, ReadOnlySequence<byte> value, DistributedCacheEntryOptions options) => SetAsync(key, value, options).AsTask().GetAwaiter().GetResult();

    public async ValueTask SetAsync(string key, ReadOnlySequence<byte> value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(options);
#else
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }
#endif

        var memoryOwner = NatsMemoryOwner<byte>.Allocate((int)value.Length);
        value.CopyTo(memoryOwner.Memory.Span);
        var ttl = ValidateCacheOptionsAndDetermineTtl(options);
        if (ttl.HasValue)
        {
            await _natsKvStore.CreateAsync(key, memoryOwner, ttl.Value, cancellationToken: token).ConfigureAwait(false);
        }
        else
        {
            await _natsKvStore.CreateAsync(key, memoryOwner, cancellationToken: token).ConfigureAwait(false);
        }
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
