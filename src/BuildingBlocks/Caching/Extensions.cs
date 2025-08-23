using BuildingBlocks.Web;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace BuildingBlocks.Caching;

public static class Extensions
{
    public static IServiceCollection AddCustomHybridCaching(this IServiceCollection services)
    {
        var redisOptions = services.GetOptions<RedisOptions>(nameof(RedisOptions));


        // Add in-memory cache for hybrid caching
        services.AddMemoryCache(options =>
                                {
                                    options.SizeLimit = 10000; // Limit number of cache entries
                                });

        // Add Redis Distributed Cache
        services.AddStackExchangeRedisCache(options =>
                                            {
                                                options.Configuration = redisOptions.ConnectionString;
                                                options.InstanceName = redisOptions.InstanceName;
                                            });

        // Add Redis Connection Multiplexer
        services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(redisOptions.ConnectionString));

        // Add Medallion Distributed Lock Provider
        services.AddSingleton<IDistributedLockProvider>(sp =>
                                                        {
                                                            var redis = sp.GetRequiredService<IConnectionMultiplexer>();
                                                            return new RedisDistributedSynchronizationProvider(redis.GetDatabase());
                                                        });

        services.AddSingleton<IHybridCacheProvider, HybridCacheProvider>();
        
        return services;
    }

}