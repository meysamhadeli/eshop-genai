using System.Text.Json;
using Medallion.Threading;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Caching;

public interface IHybridCacheProvider
{
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task<T?> GetOrSetWithLockAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, TimeSpan? lockTimeout = null, CancellationToken cancellationToken = default);
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task SetWithLockAsync<T>(string key, T value, TimeSpan? expiration = null, TimeSpan? lockTimeout = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}

public class HybridCacheProvider : IHybridCacheProvider
{
    private readonly IDistributedCache _distributedCache;
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedLockProvider _lockProvider;
    private readonly RedisOptions _redisOptions;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly TimeSpan _defaultLockTimeout = TimeSpan.FromSeconds(5);

    public HybridCacheProvider(
        IDistributedCache distributedCache, 
        IMemoryCache memoryCache,
        IDistributedLockProvider lockProvider,
        IOptions<RedisOptions> redisOptions)
    {
        _distributedCache = distributedCache;
        _memoryCache = memoryCache;
        _lockProvider = lockProvider;
        _redisOptions = redisOptions.Value;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        // First try to get from memory cache
        if (_memoryCache.TryGetValue(key, out T? memoryValue) && memoryValue != null)
        {
            return memoryValue;
        }

        // Then try to get from distributed cache
        var distributedValue = await GetFromDistributedCacheAsync<T>(key, cancellationToken);
        if (distributedValue != null)
        {
            // Cache in memory for faster access
            SetMemoryCache(key, distributedValue, expiration);
            return distributedValue;
        }

        // If not found in any cache, execute factory method
        var value = await factory();
        if (value != null)
        {
            await SetAsync(key, value, expiration, cancellationToken);
        }

        return value;
    }

    public async Task<T?> GetOrSetWithLockAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, TimeSpan? lockTimeout = null, CancellationToken cancellationToken = default)
    {
        // First try to get from memory cache
        if (_memoryCache.TryGetValue(key, out T? memoryValue) && memoryValue != null)
        {
            return memoryValue;
        }

        // Then try to get from distributed cache
        var distributedValue = await GetFromDistributedCacheAsync<T>(key, cancellationToken);
        if (distributedValue != null)
        {
            // Cache in memory for faster access
            SetMemoryCache(key, distributedValue, expiration);
            return distributedValue;
        }

        // If not found, acquire lock and execute factory method
        var lockTime = lockTimeout ?? _defaultLockTimeout;
        await using var lockHandle = await _lockProvider.TryAcquireLockAsync(GetLockKey(key), lockTime, cancellationToken);
        
        if (lockHandle == null)
            throw new System.Exception($"Could not acquire lock for cache key: {key}");

        // Double-check after acquiring lock
        distributedValue = await GetFromDistributedCacheAsync<T>(key, cancellationToken);
        if (distributedValue != null)
        {
            SetMemoryCache(key, distributedValue, expiration);
            return distributedValue;
        }

        var value = await factory();
        if (value != null)
        {
            await SetDistributedCacheAsync(key, value, expiration, cancellationToken);
            SetMemoryCache(key, value, expiration);
        }

        return value;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        // First try memory cache
        if (_memoryCache.TryGetValue(key, out T? memoryValue) && memoryValue != null)
        {
            return memoryValue;
        }

        // Then try distributed cache
        var distributedValue = await GetFromDistributedCacheAsync<T>(key, cancellationToken);
        if (distributedValue != null)
        {
            // Cache in memory for faster access
            SetMemoryCache(key, distributedValue);
        }

        return distributedValue;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        // Set in memory cache
        SetMemoryCache(key, value, expiration);

        // Set in distributed cache
        await SetDistributedCacheAsync(key, value, expiration, cancellationToken);
    }

    public async Task SetWithLockAsync<T>(string key, T value, TimeSpan? expiration = null, TimeSpan? lockTimeout = null, CancellationToken cancellationToken = default)
    {
        var lockTime = lockTimeout ?? _defaultLockTimeout;
        await using var lockHandle = await _lockProvider.TryAcquireLockAsync(GetLockKey(key), lockTime, cancellationToken);
        
        if (lockHandle == null)
            throw new System.Exception($"Could not acquire lock for cache key: {key}");

        // Set in both caches
        SetMemoryCache(key, value, expiration);
        await SetDistributedCacheAsync(key, value, expiration, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        // Remove from memory cache
        _memoryCache.Remove(key);

        // Remove from distributed cache
        await _distributedCache.RemoveAsync(key, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        // Check memory cache first
        if (_memoryCache.TryGetValue(key, out _))
        {
            return true;
        }

        // Check distributed cache
        var value = await _distributedCache.GetStringAsync(key, cancellationToken);
        return value != null;
    }

    private void SetMemoryCache<T>(string key, T value, TimeSpan? expiration = null)
    {
        var memoryCacheEntryOptions = new MemoryCacheEntryOptions
        {
            Size = 1 // Each entry has size 1 to respect SizeLimit
        };

        if (expiration.HasValue)
        {
            memoryCacheEntryOptions.SetAbsoluteExpiration(expiration.Value);
        }
        else
        {
            // Default memory cache expiration (shorter than Redis)
            memoryCacheEntryOptions.SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
        }

        _memoryCache.Set(key, value, memoryCacheEntryOptions);
    }

    private async Task SetDistributedCacheAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        var options = new DistributedCacheEntryOptions();

        // Use provided expiration, fall back to RedisOptions, then default
        var finalExpiration = expiration ?? GetDefaultExpiration();
        options.SetAbsoluteExpiration(finalExpiration);

        await _distributedCache.SetStringAsync(key, json, options, cancellationToken);
    }

    private async Task<T?> GetFromDistributedCacheAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var json = await _distributedCache.GetStringAsync(key, cancellationToken);
        if (string.IsNullOrEmpty(json))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (JsonException)
        {
            // If deserialization fails, remove the corrupted entry
            await _distributedCache.RemoveAsync(key, cancellationToken);
            return default;
        }
    }

    private TimeSpan GetDefaultExpiration()
    {
        // Use RedisOptions.ExpireSeconds if set, otherwise default to 1 hour
        if (_redisOptions.ExpireSeconds.HasValue && _redisOptions.ExpireSeconds > 0)
        {
            return TimeSpan.FromSeconds(_redisOptions.ExpireSeconds.Value);
        }
        
        return TimeSpan.FromHours(1); // Fallback default
    }

    private static string GetLockKey(string cacheKey) => $"lock:{cacheKey}";
}