using BuildingBlocks.Core.Model;

namespace Basket.Baskets.Models;

public record Basket : Entity<Guid>
{
    public string UserId { get; set; }
    public ICollection<BasketItems> Items { get; set; } = new List<BasketItems>();
}