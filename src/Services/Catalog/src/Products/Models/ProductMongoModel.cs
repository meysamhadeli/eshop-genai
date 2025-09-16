using BuildingBlocks.Core.Model;

namespace Catalog.Products.Models;

public record ProductMongoModel : Entity<Guid>
{
    public string Name { get; init; }
    public string Description { get; init; }
    public decimal Price { get; init; }
    public string ImageUrl { get; init; }
}