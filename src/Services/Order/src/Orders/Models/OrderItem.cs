namespace Order.Orders.Models;

public record OrderItem
{
    public Guid ProductId { get; set; }
    public Guid OrderId { get; set; }
    public string ProductName { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice => UnitPrice * Quantity;
    public string ImageUrl { get; set; }
}