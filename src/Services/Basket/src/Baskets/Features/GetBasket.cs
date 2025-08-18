using Basket.Baskets.Dtos;
using Basket.Data;
using BuildingBlocks.Web;
using Catalog;
using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Basket.Baskets.Features;

public record GetBasketQuery(string UserId) : IRequest<BasketDto>;

public class GetBasketQueryHandler : IRequestHandler<GetBasketQuery, BasketDto>
{
    private readonly BasketDbContext _context;
    private readonly CatalogGrpcService.CatalogGrpcServiceClient _catalogGrpcServiceClient;

    public GetBasketQueryHandler(BasketDbContext context, IMapper mapper, CatalogGrpcService.CatalogGrpcServiceClient catalogGrpcServiceClient)
    {
        _context = context;
        _catalogGrpcServiceClient = catalogGrpcServiceClient;
    }

    public async Task<BasketDto> Handle(
        GetBasketQuery request,
        CancellationToken cancellationToken)
    {
        var basket = await _context.Baskets
                         .Include(b => b.Items)
                         .FirstOrDefaultAsync(b => b.UserId == request.UserId, cancellationToken);

        BasketDto basketDto = new BasketDto(basket.Id, basket.UserId, new List<BasketItemsDto>(), (DateTime)basket.CreatedAt, basket.LastModified);

        foreach (var item in basket.Items)
        {
           var product = await _catalogGrpcServiceClient.GetProductByIdAsync(new GetProductByIdRequest(){Id = item.ProductId.ToString()});
           var basketItem = basket.Items.First(x => x.ProductId == new Guid(product.ProductDto.Id));
           basketDto.Items.Add(new BasketItemsDto(Guid.CreateVersion7(), basketItem.ProductId, product.ProductDto.Name, (decimal)product.ProductDto.Price, product.ProductDto.ImageUrl, basketItem.Quantity, DateTime.Now));
        }
        
        return basketDto;
    }
}

public class GetBasketEndpoints: IMinimalEndpoint
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