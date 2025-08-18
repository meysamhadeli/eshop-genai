// Features/Basket/AddItem/AddItemCommand.cs

using Basket.Baskets.Dtos;
using Basket.Baskets.Exceptions;
using Basket.Baskets.Models;
using Basket.Data;
using BuildingBlocks.Web;
using Catalog;
using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Basket.Baskets.Features;

public record UpdateItem(
    string UserId,
    Guid ProductId,
    int Quantity = 0) : IRequest<BasketDto>;


public class UpdateItemCommandHandler : IRequestHandler<UpdateItem, BasketDto>
{
    private readonly BasketDbContext _context;
    private readonly CatalogGrpcService.CatalogGrpcServiceClient _catalogGrpcServiceClient;
    private readonly IMapper _mapper;

    public UpdateItemCommandHandler(BasketDbContext context, IMapper mapper, CatalogGrpcService.CatalogGrpcServiceClient catalogGrpcServiceClient)
    {
        _context = context;
        _mapper = mapper;
        _catalogGrpcServiceClient = catalogGrpcServiceClient;
    }

    public async Task<BasketDto> Handle(
        UpdateItem request,
        CancellationToken cancellationToken)
    {
         // Validate input
        if (string.IsNullOrWhiteSpace(request.UserId))
            throw new ArgumentException("User ID is required.");

        if (request.Quantity < 0)
            throw new ArgumentException("Quantity cannot be negative.");

        // Load or create basket
        var basket = await _context.Baskets
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.UserId == request.UserId, cancellationToken);

        if (basket == null)
        {
            basket = new Basket.Baskets.Models.Basket { UserId = request.UserId };
            _context.Baskets.Add(basket);
        }

        var existingItem = basket.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        
        var product = await _catalogGrpcServiceClient.GetProductByIdAsync(
                new GetProductByIdRequest { Id = request.ProductId.ToString() },
                cancellationToken: cancellationToken);
  

        if (product == null)
        {
            throw new ProductNotFoundException();
        }

        if (existingItem == null)
        {
            if (request.Quantity > 0)
            {
                basket.Items.Add(new BasketItems
                {
                    ProductId = request.ProductId,
                    Quantity = request.Quantity,
                    BasketId = basket.Id,
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

        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<BasketDto>(basket);
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