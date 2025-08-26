using MediatR;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Caching;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
    where TResponse : notnull
{
    private readonly IHybridCacheProvider _cacheProvider;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;
    private readonly TimeSpan _defaultCacheExpiration = TimeSpan.FromHours(1);
    private readonly TimeSpan _defaultLockTimeout = TimeSpan.FromSeconds(5);

    public CachingBehavior(
        IHybridCacheProvider cacheProvider,
        ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cacheProvider = cacheProvider;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Check for different cache request types
        if (request is ICacheRequestWithLock cacheRequestWithLock)
        {
            return await HandleWithLock(cacheRequestWithLock, next, cancellationToken);
        }

        if (request is ICacheRequest cacheRequest)
        {
            return await HandleWithoutLock(cacheRequest, next, cancellationToken);
        }

        // No cache request found, continue through pipeline
        return await next();
    }

    private async Task<TResponse> HandleWithLock(ICacheRequestWithLock cacheRequest, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var cacheKey = cacheRequest.CacheKey;
        var expiration = CalculateExpiration(cacheRequest.AbsoluteExpirationRelativeToNow);
        var lockTimeout = cacheRequest.LockTimeout ?? _defaultLockTimeout;

        // Use distributed locking for cache population
        var response = await _cacheProvider.GetOrSetWithLockAsync(
            cacheKey,
            async () => await next(),
            expiration,
            lockTimeout,
            cancellationToken);

        _logger.LogDebug("Response for {TRequest} handled with distributed locking, cache key: {CacheKey}",
            typeof(TRequest).FullName, cacheKey);

        return response;
    }

    private async Task<TResponse> HandleWithoutLock(ICacheRequest cacheRequest, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var cacheKey = cacheRequest.CacheKey;
        var expiration = CalculateExpiration(cacheRequest.AbsoluteExpirationRelativeToNow);

        // Use normal caching without distributed locking
        var response = await _cacheProvider.GetOrSetAsync(
            cacheKey,
            async () => await next(),
            expiration,
            cancellationToken);

        _logger.LogDebug("Response for {TRequest} handled with cache key: {CacheKey}",
            typeof(TRequest).FullName, cacheKey);

        return response;
    }

    private TimeSpan CalculateExpiration(DateTime? absoluteExpiration)
    {
        if (absoluteExpiration.HasValue)
        {
            var timeUntilExpiration = absoluteExpiration.Value - DateTime.Now;
            return timeUntilExpiration > TimeSpan.Zero ? timeUntilExpiration : TimeSpan.Zero;
        }

        return _defaultCacheExpiration;
    }
}