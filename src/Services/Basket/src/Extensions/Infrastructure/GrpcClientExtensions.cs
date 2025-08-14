using Basket.Baskets.Configuration;
using BuildingBlocks.Web;
using Catalog;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace Booking.Extensions.Infrastructure;

public static class GrpcClientExtensions
{
    public static IServiceCollection AddGrpcClients(this IServiceCollection services)
    {
        var grpcOptions = services.GetOptions<GrpcOptions>("Grpc");
        var resilienceOptions = services.GetOptions<HttpStandardResilienceOptions>(nameof(HttpStandardResilienceOptions));

        services.AddGrpcClient<CatalogGrpcService.CatalogGrpcServiceClient>(o =>
                                                                            {
                                                                                o.Address = new Uri(
                                                                                    grpcOptions.CatalogAddress);
                                                                            });
        
        return services;
    }
}