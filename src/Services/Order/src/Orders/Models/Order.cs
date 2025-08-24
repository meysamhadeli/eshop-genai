using BuildingBlocks.Core.Model;
using Order.Orders.Enums;

namespace Order.Orders.Models;

public record Order : Entity<Guid>
{
    public string UserId { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string ShippingAddress { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
}