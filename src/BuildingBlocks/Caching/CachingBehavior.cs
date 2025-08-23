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
        if (request is not ICacheRequest cacheRequest)
            // No cache request found, so just continue through the pipeline
            return await next();

        var cacheKey = cacheRequest.CacheKey;
        
        // Try to get from cache first
        var cachedResponse = await _cacheProvider.GetAsync<TResponse>(cacheKey, cancellationToken);
        if (cachedResponse != null)
        {
            _logger.LogDebug("Response retrieved {TRequest} from cache. CacheKey: {CacheKey}",
                typeof(TRequest).FullName, cacheKey);
            return cachedResponse;
        }

        // If not in cache, execute the request
        var response = await next();

        // Convert DateTime? to TimeSpan for the cache provider
        TimeSpan expiration = CalculateExpiration(cacheRequest.AbsoluteExpirationRelativeToNow);

        // Cache the response with locking to prevent race conditions
        await _cacheProvider.SetWithLockAsync(cacheKey, response, expiration, cancellationToken: cancellationToken);

        _logger.LogDebug("Caching response for {TRequest} with cache key: {CacheKey}", typeof(TRequest).FullName,
            cacheKey);

        return response;
    }

    private TimeSpan CalculateExpiration(DateTime? absoluteExpiration)
    {
        if (absoluteExpiration.HasValue)
        {
            // Calculate the duration from now until the specified DateTime
            var timeUntilExpiration = absoluteExpiration.Value - DateTime.Now;
            
            // Ensure we don't return negative time spans
            return timeUntilExpiration > TimeSpan.Zero ? timeUntilExpiration : TimeSpan.Zero;
        }
        
        return _defaultCacheExpiration;
    }
}