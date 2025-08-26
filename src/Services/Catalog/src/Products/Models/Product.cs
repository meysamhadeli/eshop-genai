using BuildingBlocks.Core.Model;

namespace Catalog.Products.Models;

public record Product : Entity<Guid>
{
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string ImageUrl { get; set; }
}