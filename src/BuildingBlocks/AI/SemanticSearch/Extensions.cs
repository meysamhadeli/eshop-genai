using BuildingBlocks.SemanticSearch;
using BuildingBlocks.Web;
using Microsoft.Extensions.DependencyInjection;
using Qdrant.Client;

namespace BuildingBlocks.AI.SemanticSearch;

public static class Extensions
{
    public static IServiceCollection AddSemanticSearch(this IServiceCollection services)
    {
        services.AddValidateOptions<SemanticSearchOptions>();

        var options = services.GetOptions<SemanticSearchOptions>(nameof(SemanticSearchOptions));

        services.AddSingleton(new QdrantClient(new Uri(options.VectorDbConnectionString)));
        services.AddSingleton(_=> AIProviderFactory.RegisterEmbeddingProviders(options));
        services.AddSingleton<ISemanticSearchService, SemanticSearchService>();

        return services;
    }
}