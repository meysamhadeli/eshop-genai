using BuildingBlocks.Core.Model;

namespace Recommendation.Recommendations.Models;

public record ProductQdrantModel : Entity<Guid>
{
    public string Name { get; init; }
    public string Description { get; init; }
    public decimal Price { get; init; }
    public string ImageUrl { get; init; }
}