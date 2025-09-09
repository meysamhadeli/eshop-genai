using BuildingBlocks.AI.SemanticSearch;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Recommendation;

public static class Extensions
{
    public static IServiceCollection AddRecommendationService(this IServiceCollection services)
    {
        services.AddSingleton<IRecommendationService, RecommendationService>();
        return services;
    }
}