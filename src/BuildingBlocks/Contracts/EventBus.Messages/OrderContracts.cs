using BuildingBlocks.Core.Event;

namespace BuildingBlocks.Contracts.EventBus.Messages;

public record OrderCreatedIntegrationEvent(Guid Id, string UserId, OrderStatusIntegrationEvent Status, decimal TotalAmount, string ShippingAddress,
                                           DateTime OrderDate, ICollection<OrderItemIntegrationEvent> Items, bool IsDeleted) : IIntegrationEvent;

public record OrderItemIntegrationEvent
{
    public Guid ProductId { get; set; }
    public Guid OrderId { get; set; }
    public string ProductName { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public string ImageUrl { get; set; }
} 

public enum OrderStatusIntegrationEvent
{
    Pending,
    Confirmed,
    Paid,
    Shipped,
    Delivered,
    Cancelled
}