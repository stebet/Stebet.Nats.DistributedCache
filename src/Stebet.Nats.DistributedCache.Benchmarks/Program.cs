using BenchmarkDotNet.Running;

namespace Stebet.Nats.DistributedCache.Benchmarks;

public static class Program
{
    public static void Main()
    {
        BenchmarkRunner.Run<NatsBenchmarks>();
    }
}