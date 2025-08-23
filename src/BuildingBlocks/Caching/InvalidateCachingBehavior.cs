using MediatR;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Caching;

public class InvalidateCachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
where TRequest : notnull, IRequest<TResponse>
where TResponse : notnull
{
    private readonly ILogger<InvalidateCachingBehavior<TRequest, TResponse>> _logger;
    private readonly IHybridCacheProvider _cacheProvider;

    public InvalidateCachingBehavior(
        IHybridCacheProvider cacheProvider,
        ILogger<InvalidateCachingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
        _cacheProvider = cacheProvider;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not IInvalidateCacheRequest invalidateCacheRequest)
        {
            // No cache request found, so just continue through the pipeline
            return await next();
        }

        var cacheKey = invalidateCacheRequest.CacheKey;
        
        // Execute the request first
        var response = await next();

        // Remove from cache after successful execution
        await _cacheProvider.RemoveAsync(cacheKey, cancellationToken);

        _logger.LogDebug("Cache data with cache key: {CacheKey} removed.", cacheKey);

        return response;
    }
}