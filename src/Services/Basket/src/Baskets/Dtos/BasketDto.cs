namespace Basket.Baskets.Dtos;

public record BasketDto(
    Guid Id,
    string UserId,
    List<BasketItemsDto> Items,
    DateTime CreatedAt,
    DateTime? LastModified,
    DateTime? ExpirationTime);

public record BasketItemsDto(
    Guid ProductId,
    string ProductName,
    decimal ProductPrice,
    string ProductImageUrl,
    int Quantity);