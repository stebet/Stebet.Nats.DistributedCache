using Microsoft.Extensions.Options;

namespace Stebet.Nats.DistributedCache;

public class NatsDistributedCacheOptions : IOptions<NatsDistributedCacheOptions>
{
    public NatsDistributedCacheOptions Value => this;

    /// <summary>
    /// The name of the bucket to store the cache in. Defaults to "NatsDistributedCache".
    /// </summary>
    public string BucketName { get; set; } = "NatsDistributedCache";
}
