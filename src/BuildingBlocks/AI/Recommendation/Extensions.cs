using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.AI.Recommendation;

public static class Extensions
{
    public static IServiceCollection AddRecommendationService(this IServiceCollection services)
    {
        services.AddScoped<IRecommendationService, RecommendationService>();
        return services;
    }
}