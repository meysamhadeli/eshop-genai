namespace Basket.Baskets.Models;

public record BasketItems
{
    public Guid BasketId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}