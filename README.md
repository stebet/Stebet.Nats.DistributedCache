# Stebet.Nats.DistributedCache

[![codecov](https://codecov.io/gh/stebet/Stebet.Nats.DistributedCache/graph/badge.svg?token=rjCaeMRBsa)](https://codecov.io/gh/stebet/Stebet.Nats.DistributedCache)
[![NuGet](https://img.shields.io/nuget/v/Stebet.Nats.DistributedCache.svg)](https://www.nuget.org/packages/Stebet.Nats.DistributedCache)
[![License](https://img.shields.io/github/license/stebet/Stebet.Nats.DistributedCache)](https://github.com/stebet/Stebet.Nats.DistributedCache/blob/master/LICENSE.txt)

A distributed cache implementation for .NET Core using NATS 2.11 or higher as the backing store. This package provides a simple way to integrate NATS-based distributed caching into your ASP.NET Core applications.

## Features

- Implementation of the `IDistributedCache` interface
  - **REQUIRES NATS Server 2.11 OR HIGHER**
  - **Sliding expiration is not currently supported**

## Installation

Install the package from NuGet:

```
dotnet add package Stebet.Nats.DistributedCache
```

Or via the NuGet Package Manager:

```
Install-Package Stebet.Nats.DistributedCache
```

## Basic Usage

### Add to services in Program.cs or Startup.cs

```csharp
using Stebet.Nats.DistributedCache;

...
// Add a NATS connection (see https://www.nuget.org/packages/NATS.Extensions.Microsoft.DependencyInjection)
builder.services.AddNatsClient(nats => nats.ConfigureOptions(opts => opts with { Url = "nats://localhost:4222" }));

// Add NATS distributed cache
builder.Services.AddNatsDistributedCache(options =>
{
    options.BucketName = "MyCacheBucket";
});
...
```

### Use the cache in your controllers or services

```csharp
using Microsoft.Extensions.Caching.Distributed;

public class WeatherController : Controller
{
    private readonly IDistributedCache _cache;

    public WeatherController(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<IActionResult> GetForecast(string location)
    {
        var cacheKey = $"weather:{location}";
        
        // Try to get from cache first
        var cachedForecast = await _cache.GetStringAsync(cacheKey);
        if (cachedForecast != null)
        {
            return Ok(JsonSerializer.Deserialize<WeatherForecast>(cachedForecast));
        }
        
        // Cache miss - get from service
        var forecast = await _weatherService.GetForecastAsync(location);
        
        // Save to cache with expiration
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };
        
        await _cache.SetStringAsync(
            cacheKey, 
            JsonSerializer.Serialize(forecast),
            options);
            
        return Ok(forecast);
    }
}
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.