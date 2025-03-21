using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NATS.Client.Core;

namespace Stebet.Nats.DistributedCache.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddNatsDistributedCache_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSingleton<INatsConnection>(new NatsConnection());
        services.AddNatsDistributedCache(options => options.BucketName = "TestBucket");

        // Assert
        using var serviceProvider = services.BuildServiceProvider();

        var cache = serviceProvider.GetService<IDistributedCache>();
        Assert.NotNull(cache);
        Assert.IsType<NatsDistributedCache>(cache);

        var options = serviceProvider.GetService<IOptions<NatsDistributedCacheOptions>>();
        Assert.NotNull(options);
        Assert.Equal("TestBucket", options.Value.BucketName);
    }

    [Fact]
    public void AddNatsDistributedCache_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            services.AddNatsDistributedCache(options => options.BucketName = "TestBucket"));

        Assert.Equal("services", exception.ParamName);
    }

    [Fact]
    public void AddNatsDistributedCache_NullSetupAction_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<NatsDistributedCacheOptions> setupAction = null!;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            services.AddNatsDistributedCache(setupAction));

        Assert.Equal("configureOptions", exception.ParamName);
    }

    [Fact]
    public void AddNatsDistributedCache_RegistersServicesAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNatsDistributedCache(options => options.BucketName = "TestBucket");

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IDistributedCache));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(NatsDistributedCache), descriptor.ImplementationType);
    }

    [Fact]
    public void AddNatsDistributedCache_MultipleRegistrations_RegistersOnce()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSingleton<INatsConnection>(new NatsConnection());
        services.AddNatsDistributedCache(options => options.BucketName = "TestBucket1");
        services.AddNatsDistributedCache(options => options.BucketName = "TestBucket2");

        // Assert
        var registrations = services.Count(d => d.ServiceType == typeof(IDistributedCache));
        Assert.Equal(1, registrations);

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptions<NatsDistributedCacheOptions>>();
        Assert.Equal("TestBucket2", options!.Value.BucketName);
    }
}
