using BuildingBlocks.Caching;

namespace Basket.Infrastructure.Redis;

public interface IBasketRedisService
{
    Task<Baskets.Models.Basket?> GetBasketAsync(string userId, CancellationToken cancellationToken = default);
    Task<Baskets.Models.Basket> SaveBasketAsync(Baskets.Models.Basket basket, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task<bool> ClearBasketAsync(string userId, CancellationToken cancellationToken = default);
}

public class BasketRedisService : IBasketRedisService
{
    private readonly IHybridCacheProvider _hybridCacheProvider;
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(1);

    public BasketRedisService(IHybridCacheProvider hybridCacheProvider)
    {
        _hybridCacheProvider = hybridCacheProvider;
    }

    public async Task<Baskets.Models.Basket?> GetBasketAsync(string userId, CancellationToken cancellationToken = default)
    {
        var key = GetBasketKey(userId);
        return await _hybridCacheProvider.GetAsync<Baskets.Models.Basket>(key, cancellationToken);
    }

    public async Task<Baskets.Models.Basket> SaveBasketAsync(Baskets.Models.Basket basket, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var key = GetBasketKey(basket.UserId);
        var expiration = expiry ?? _defaultExpiration;
        
        basket.ExpirationTime = DateTime.UtcNow.Add(expiration);
        basket.LastModified = DateTime.UtcNow;

        // Use the locked version to prevent race conditions
        await _hybridCacheProvider.SetWithLockAsync(key, basket, expiration, cancellationToken:cancellationToken);
        return basket;
    }

    public async Task<bool> ClearBasketAsync(string userId, CancellationToken cancellationToken = default)
    {
        var key = GetBasketKey(userId);
        await _hybridCacheProvider.RemoveAsync(key, cancellationToken);
        return true;
    }

    private static string GetBasketKey(string userId) => $"basket:{userId}";
}