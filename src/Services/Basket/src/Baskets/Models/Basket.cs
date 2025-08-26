using BuildingBlocks.Core.Model;

namespace Basket.Baskets.Models;

public record Basket : Entity<Guid>
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public ICollection<BasketItems> Items { get; set; } = new List<BasketItems>();
    public DateTime? ExpirationTime { get; set; }
}