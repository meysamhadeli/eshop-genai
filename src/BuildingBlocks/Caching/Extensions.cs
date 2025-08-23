using BuildingBlocks.Web;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace BuildingBlocks.Caching;

public static class Extensions
{
    public static IServiceCollection AddCustomHybridCaching(this IServiceCollection services)
    {
        var hybridCacheOptions = services.GetOptions<HybridCacheOptions>(nameof(HybridCacheOptions));

        services.AddHybridCache(options =>
                                {
                                    options.DefaultEntryOptions = new HybridCacheEntryOptions
                                                                  {
                                                                      Expiration =TimeSpan.FromMinutes(hybridCacheOptions.RedisExpireMinutes ?? 5),
                                                                      LocalCacheExpiration = TimeSpan.FromMinutes(hybridCacheOptions.InMemoryExpireMinutes ?? 2)
                                                                  };
                                });

        if (!string.IsNullOrEmpty(hybridCacheOptions.RedisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
                                                {
                                                    options.Configuration = hybridCacheOptions.RedisConnectionString;
                                                    options.InstanceName = hybridCacheOptions.InstanceName;
                                                });

            services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(hybridCacheOptions.RedisConnectionString));

            // Add distributed lock provider for Redis
            if (hybridCacheOptions.UseDistributedLock)
            {
                services.AddSingleton<IDistributedLockProvider>(sp =>
                                                                {
                                                                    var redis = sp.GetRequiredService<IConnectionMultiplexer>();
                                                                    return new RedisDistributedSynchronizationProvider(redis.GetDatabase());
                                                                });
            }
        }

        services.AddSingleton<IHybridCacheProvider, HybridCacheProvider>();
        
        return services;
    }
}