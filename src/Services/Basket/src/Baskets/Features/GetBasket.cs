using Basket.Baskets.Dtos;
using Basket.Infrastructure.Redis;
using BuildingBlocks.Web;
using Catalog;
using MediatR;

namespace Basket.Baskets.Features;

public record GetBasketQuery(string UserId) : IRequest<BasketDto>;

public class GetBasketQueryHandler : IRequestHandler<GetBasketQuery, BasketDto>
{
    private readonly IBasketRedisService _basketRedisService;
    private readonly CatalogGrpcService.CatalogGrpcServiceClient _catalogGrpcServiceClient;

    public GetBasketQueryHandler(
        IBasketRedisService basketRedisService,
        CatalogGrpcService.CatalogGrpcServiceClient catalogGrpcServiceClient)
    {
        _basketRedisService = basketRedisService;
        _catalogGrpcServiceClient = catalogGrpcServiceClient;
    }

    public async Task<BasketDto> Handle(
        GetBasketQuery request,
        CancellationToken cancellationToken)
    {
        // Get basket from Redis
        var basket = await _basketRedisService.GetBasketAsync(request.UserId, cancellationToken);

        // If basket doesn't exist, return an empty basket
        if (basket == null)
        {
            return new BasketDto(
                request.UserId,
                new List<BasketItemsDto>(),
                DateTime.UtcNow,
                null,
                null);
        }

        BasketDto basketDto = new BasketDto(basket.UserId, new List<BasketItemsDto>(), (DateTime)basket.CreatedAt, basket.LastModified, basket.ExpirationTime);

        foreach (var item in basket.Items)
        {
            var product = await _catalogGrpcServiceClient.GetProductByIdAsync(new GetProductByIdRequest(){Id = item.ProductId.ToString()});
            var basketItem = basket.Items.First(x => x.ProductId == new Guid(product.ProductDto.Id));
            basketDto.Items.Add(new BasketItemsDto( basketItem.ProductId, product.ProductDto.Name, (decimal)product.ProductDto.Price, product.ProductDto.ImageUrl, basketItem.Quantity, DateTime.Now));
        }


        return basketDto;
    }
}

public class GetBasketEndpoints : IMinimalEndpoint
{
    public IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapGet($"{EndpointConfig.BaseApiPath}/basket", async (
                                                                   [AsParameters] GetBasketQuery query,
                                                                   IMediator mediator,
                                                                   CancellationToken cancellationToken) =>
                                                               {
                                                                   var result = await mediator.Send(query, cancellationToken);
                                                                   return Results.Ok(result);
                                                               })
            .WithName("GetBasket")
            .WithApiVersionSet(builder.NewApiVersionSet("Basket").Build())
            .Produces<BasketDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithSummary("Get Basket")
            .WithDescription("Get Basket Items")
            .WithOpenApi()
            .HasApiVersion(1.0);
        
        return builder;
    }
}