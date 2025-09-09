using BuildingBlocks.SemanticSearch;
using BuildingBlocks.Web;
using Microsoft.Extensions.DependencyInjection;
using Qdrant.Client;

namespace BuildingBlocks.AI.SemanticSearch;

public static class SemanticSearchExtensions
{
    public static IServiceCollection AddSemanticSearch(this IServiceCollection services)
    {
        services.AddValidateOptions<AIOptions>();
        
        var options = services.GetOptions<AIOptions>(nameof(AIOptions));
        
        services.AddSingleton<QdrantClient>(sp => new QdrantClient(new Uri(options.VectorDbConnectionString)));
        services.AddSingleton<ISemanticSearchService, SemanticSearchService>();
        
        return services;
    }
}