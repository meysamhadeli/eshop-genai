using Order.Orders.Enums;

namespace Order.Orders.Dtos;

public record OrderDto(
    Guid Id,
    string UserId,
    OrderStatus Status,
    decimal TotalAmount,
    string ShippingAddress,
    List<OrderItemDto> Items,
    DateTime OrderDate,
    DateTime CreatedAt,
    DateTime? LastModified);


public record OrderItemDto(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice,
    string ImageUrl);