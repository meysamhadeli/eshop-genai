using BuildingBlocks.Core.Model;

namespace Basket.Baskets.Models;

public record BasketItems: Entity<Guid>
{
    public Guid BasketId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}