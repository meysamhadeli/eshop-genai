using System.Text.Json;
using Medallion.Threading;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Caching;

public interface IHybridCacheProvider
{
    /// <summary>
    /// Gets a value from cache or sets it using the factory method if not found.
    /// Uses .NET HybridCache's built-in stampede protection.
    /// </summary>
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value from cache or sets it using the factory method with distributed locking.
    /// Useful for expensive operations that should not be executed concurrently across multiple instances.
    /// </summary>
    Task<T?> GetOrSetWithLockAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, TimeSpan? lockTimeout = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value from cache without executing a factory method.
    /// Returns default(T) if the key does not exist.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a value in cache with the specified expiration time.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a value in cache with distributed locking to prevent concurrent writes.
    /// </summary>
    Task SetWithLockAsync<T>(string key, T value, TimeSpan? expiration = null, TimeSpan? lockTimeout = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a value from cache.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a key exists in cache.
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}

public class HybridCacheProvider : IHybridCacheProvider
{
    private readonly HybridCache _hybridCache;
    private readonly IDistributedLockProvider? _lockProvider;
    private readonly HybridCacheOptions _hybridCacheOptions;
    private readonly TimeSpan _defaultLockTimeout = TimeSpan.FromSeconds(5);
    private readonly bool _redisEnabled;
    private readonly bool _useDistributedLock;

    public HybridCacheProvider(
        HybridCache hybridCache,
        IDistributedLockProvider lockProvider,
        IOptions<HybridCacheOptions> hybridCacheOptions)
    {
        _hybridCache = hybridCache;
        _lockProvider = lockProvider;
        _hybridCacheOptions = hybridCacheOptions.Value;
        
        _redisEnabled = !string.IsNullOrEmpty(_hybridCacheOptions.RedisConnectionString);
        _useDistributedLock = _redisEnabled && _hybridCacheOptions.UseDistributedLock && _lockProvider != null;
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        // Use .NET HybridCache's GetOrCreateAsync
        return await _hybridCache.GetOrCreateAsync(
            key,
            async cancel => await factory(),
            new HybridCacheEntryOptions { Expiration = expiration ?? GetDefaultExpiration() },
            cancellationToken: cancellationToken);
    }

    public async Task<T?> GetOrSetWithLockAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, TimeSpan? lockTimeout = null, CancellationToken cancellationToken = default)
    {
        if (!_useDistributedLock)
        {
            return await GetOrSetAsync(key, factory, expiration, cancellationToken);
        }

        // First try to get the value without creating it
        var cachedValue = await GetAsync<T>(key, cancellationToken);
        if (cachedValue != null)
        {
            return cachedValue;
        }

        // Use distributed locking for cache population
        var lockTime = lockTimeout ?? _defaultLockTimeout;
        await using var lockHandle = await _lockProvider!.TryAcquireLockAsync(GetLockKey(key), lockTime, cancellationToken);
        
        if (lockHandle == null)
            throw new System.Exception($"Could not acquire lock for cache key: {key}");

        // Double-check after acquiring lock
        cachedValue = await GetAsync<T>(key, cancellationToken);
        if (cachedValue != null)
        {
            return cachedValue;
        }

        // Execute factory method and cache the result
        var value = await factory();
        if (value != null)
        {
            await SetAsync(key, value, expiration, cancellationToken);
        }

        return value;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        // For getting without setting, we use GetOrCreateAsync with a factory that returns default
        // We need to specify the type explicitly and use ValueTask
        return await _hybridCache.GetOrCreateAsync<T>(
            key,
            static async cancel => default,
            cancellationToken: cancellationToken);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        await _hybridCache.SetAsync(
            key,
            value,
            new HybridCacheEntryOptions { Expiration = expiration ?? GetDefaultExpiration() }, cancellationToken: cancellationToken);
    }

    public async Task SetWithLockAsync<T>(string key, T value, TimeSpan? expiration = null, TimeSpan? lockTimeout = null, CancellationToken cancellationToken = default)
    {
        if (!_useDistributedLock)
        {
            await SetAsync(key, value, expiration, cancellationToken);
            return;
        }

        var lockTime = lockTimeout ?? _defaultLockTimeout;
        await using var lockHandle = await _lockProvider!.TryAcquireLockAsync(GetLockKey(key), lockTime, cancellationToken);
        
        if (lockHandle == null)
            throw new System.Exception($"Could not acquire lock for cache key: {key}");

        await _hybridCache.SetAsync(
            key,
            value,
            new HybridCacheEntryOptions { Expiration = expiration ?? GetDefaultExpiration() }, cancellationToken: cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _hybridCache.RemoveAsync(key, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        // Use GetAsync to check if a value exists (if it returns non-null, the key exists)
        var result = await GetAsync<object>(key, cancellationToken);
        return result != null;
    }

    private TimeSpan GetDefaultExpiration()
    {
        if (_hybridCacheOptions.RedisExpireMinutes is > 0)
        {
            return TimeSpan.FromSeconds(_hybridCacheOptions.RedisExpireMinutes ?? 5);
        }
        
        return TimeSpan.FromHours(_hybridCacheOptions.InMemoryExpireMinutes ?? 2);
    }

    private static string GetLockKey(string cacheKey) => $"lock:{cacheKey}";
}