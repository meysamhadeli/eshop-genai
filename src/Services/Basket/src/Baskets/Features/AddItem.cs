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

public record AddItem(
    string UserId,
    Guid ProductId,
    int Quantity = 1) : IRequest<BasketDto>;


public class AddItemCommandHandler : IRequestHandler<AddItem, BasketDto>
{
    private readonly BasketDbContext _context;
    private readonly CatalogGrpcService.CatalogGrpcServiceClient _catalogGrpcServiceClient;
    private readonly IMapper _mapper;

    public AddItemCommandHandler(BasketDbContext context, IMapper mapper, CatalogGrpcService.CatalogGrpcServiceClient catalogGrpcServiceClient)
    {
        _context = context;
        _mapper = mapper;
        _catalogGrpcServiceClient = catalogGrpcServiceClient;
    }

    public async Task<BasketDto> Handle(
        AddItem request,
        CancellationToken cancellationToken)
    {
        var basket = await _context.Baskets
                         .Include(b => b.Items)
                         .FirstOrDefaultAsync(b => b.UserId == request.UserId, cancellationToken) 
                     ?? new Basket.Baskets.Models.Basket { UserId = request.UserId };

        var product =  _catalogGrpcServiceClient.GetProductById(new GetProductByIdRequest{Id = request.ProductId.ToString()});

        if (product == null)
        {
            throw new ProductNotFoundException();
        }

        var existingItem = basket?.Items?.FirstOrDefault(i => i.ProductId == request.ProductId);

        if (existingItem == null)
        {
            basket.Items.Add(new BasketItems
                             {
                                 ProductId = request.ProductId,
                                 Quantity = request.Quantity,
                                 BasketId = basket.Id,
                             });
            
            _context.Baskets.Update(basket);
        }
        else if (existingItem != null)
        {
            existingItem.Quantity += request.Quantity;
            _context.Baskets.Update(basket);
        }
        
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<BasketDto>(basket);
    }
}


public class AddBasketEndpoints: IMinimalEndpoint
{
    public IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPost($"{EndpointConfig.BaseApiPath}/basket", async (
                                                                    AddItem command,
                                                                    IMediator mediator,
                                                                    CancellationToken cancellationToken) =>
                                                                {
                                                                    var result = await mediator.Send(command, cancellationToken);
                                                                    return Results.Ok(result);
                                                                })
            .WithName("AddItem")
            .WithApiVersionSet(builder.NewApiVersionSet("Basket").Build())
            .Produces<BasketDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithSummary("Add Item")
            .WithDescription("Add Item to Basket")
            .WithOpenApi()
            .HasApiVersion(1.0);
        
        return builder;
    }
}