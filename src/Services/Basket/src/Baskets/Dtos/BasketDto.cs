namespace Basket.Baskets.Dtos;

public record BasketDto(
    Guid Id,
    string UserId,
    List<BasketItemsDto> Items,
    DateTime CreatedAt,
    DateTime? LastModified);

public record BasketItemsDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal ProductPrice,
    string ProductImageUrl,
    int Quantity,
    DateTime CreatedAt);