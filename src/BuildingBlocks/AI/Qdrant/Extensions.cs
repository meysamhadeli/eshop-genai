using BuildingBlocks.Web;
using Microsoft.Extensions.DependencyInjection;
using Qdrant.Client;

namespace BuildingBlocks.AI.Qdrant;

public static class Extensions
{
    public static IServiceCollection AddQdrant(this IServiceCollection services)
    {
        services.AddValidateOptions<AIOptions>();

        var options = services.GetOptions<AIOptions>(nameof(AIOptions));

        services.AddSingleton<QdrantClient>(sp => new QdrantClient(new Uri(options.VectorDbConnectionString)));
        services.AddSingleton(typeof(IQdrantRepository<>), typeof(QdrantRepository<>));

        return services;
    }
}