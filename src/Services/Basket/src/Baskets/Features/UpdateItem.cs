using Basket.Baskets.Dtos;
using Basket.Baskets.Exceptions;
using Basket.Baskets.Models;
using Basket.Infrastructure.Redis;
using BuildingBlocks.Web;
using Catalog;
using MapsterMapper;
using MediatR;

namespace Basket.Baskets.Features;

public record UpdateItem(
    string UserId,
    Guid ProductId,
    int Quantity = 0) : IRequest<BasketDto>;

public class UpdateItemCommandHandler : IRequestHandler<UpdateItem, BasketDto>
{
    private readonly IBasketRedisService _basketRedisService;
    private readonly CatalogGrpcService.CatalogGrpcServiceClient _catalogGrpcService;
    private readonly IMapper _mapper;
    private readonly TimeSpan _basketExpiry = TimeSpan.FromHours(1);

    public UpdateItemCommandHandler(
        IBasketRedisService basketRedisService,
        CatalogGrpcService.CatalogGrpcServiceClient catalogGrpcService,
        IMapper mapper)
    {
        _basketRedisService = basketRedisService;
        _catalogGrpcService = catalogGrpcService;
        _mapper = mapper;
    }

    public async Task<BasketDto> Handle(UpdateItem request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
            throw new ArgumentException("User ID is required.");

        if (request.Quantity < 0)
            throw new ArgumentException("Quantity cannot be negative.");

        // Validate product exists
        var productResponse = await _catalogGrpcService.GetProductByIdAsync(
            new GetProductByIdRequest { Id = request.ProductId.ToString() },
            cancellationToken: cancellationToken);

        if (productResponse?.ProductDto == null)
            throw new ProductNotFoundException();
        

        // Get or create basket
        var basket = await _basketRedisService.GetBasketAsync(request.UserId, cancellationToken) 
                     ?? new Models.Basket 
                     { 
                         Id = Guid.NewGuid(), 
                         UserId = request.UserId, 
                         CreatedAt = DateTime.UtcNow 
                     };

        // Update basket items
        var existingItem = basket.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

        if (existingItem == null)
        {
            if (request.Quantity > 0)
            {
                basket.Items.Add(new BasketItems()
                {
                    ProductId = request.ProductId,
                    Quantity = request.Quantity
                });
            }
        }
        else
        {
            if (request.Quantity > 0)
            {
                existingItem.Quantity = request.Quantity;
            }
            else
            {
                basket.Items.Remove(existingItem);
            }
        }

        basket.LastModified = DateTime.UtcNow;

        // Save basket with TTL
        var updatedBasket = await _basketRedisService.SaveBasketAsync(basket, _basketExpiry, cancellationToken);

        return _mapper.Map<BasketDto>(updatedBasket);
    }
}

public class AddBasketEndpoints: IMinimalEndpoint
{
    public IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPut($"{EndpointConfig.BaseApiPath}/basket", async (
                                                                   UpdateItem command,
                                                                   IMediator mediator,
                                                                   CancellationToken cancellationToken) =>
                                                               {
                                                                   var result = await mediator.Send(command, cancellationToken);
                                                                   return Results.Ok(result);
                                                               })
            .WithName("UpdateItem")
            .WithApiVersionSet(builder.NewApiVersionSet("Basket").Build())
            .Produces<BasketDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithSummary("Update Item")
            .WithDescription("Update Basket Items")
            .WithOpenApi()
            .HasApiVersion(1.0);
        
        return builder;
    }
}