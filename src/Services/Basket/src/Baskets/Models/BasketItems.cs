namespace Basket.Baskets.Models;

public record BasketItems
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}