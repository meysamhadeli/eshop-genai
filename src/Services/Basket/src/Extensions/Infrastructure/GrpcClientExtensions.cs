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
                                                                            })
            .AddResilienceHandler(
                "grpc-catalog-resilience",
                options =>
                {
                    var timeSpan = TimeSpan.FromMinutes(1);

                    options.AddRetry(
                        new HttpRetryStrategyOptions
                        {
                            MaxRetryAttempts = 3,
                        });

                    options.AddCircuitBreaker(
                        new HttpCircuitBreakerStrategyOptions
                        {
                            SamplingDuration = timeSpan * 2,
                        });

                    options.AddTimeout(
                        new HttpTimeoutStrategyOptions
                        {
                            Timeout = timeSpan * 3,
                        });
                });
        
        return services;
    }
}