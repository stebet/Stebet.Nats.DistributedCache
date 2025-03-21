using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Stebet.Nats.DistributedCache;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Extensions for adding NATS distributed cache to IServiceCollection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds NATS distributed cache services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="setupAction">An <see cref="Action{NatsDistributedCacheOptions}"/> to configure the provided options.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddNatsDistributedCache(this IServiceCollection services, Action<NatsDistributedCacheOptions> setupAction)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(services);
#else
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }
#endif

        // Add NATS distributed cache options
        services.Configure(setupAction);

        // Register the distributed cache implementations
        services.TryAddSingleton<IDistributedCache, NatsDistributedCache>();

        return services;
    }
}
