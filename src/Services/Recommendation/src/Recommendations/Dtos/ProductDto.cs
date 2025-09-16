namespace Recommendation.Recommendations.Dtos;

public record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string ImageUrl,
    DateTime CreatedAt,
    DateTime? LastModified);