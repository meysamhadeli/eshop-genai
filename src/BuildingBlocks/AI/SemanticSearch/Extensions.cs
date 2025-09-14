using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.AI.SemanticSearch;

public static class Extensions
{
    public static IServiceCollection AddSemanticSearch(this IServiceCollection services)
    {
        services.AddScoped<ISemanticSearchService, SemanticSearchService>();
        return services;
    }
}