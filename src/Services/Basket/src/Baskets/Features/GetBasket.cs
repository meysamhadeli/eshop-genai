using Basket.Baskets.Dtos;
using Basket.Data;
using BuildingBlocks.Web;
using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Basket.Baskets.Features;

public record GetBasketQuery(string UserId) : IRequest<BasketDto>;

public class GetBasketQueryHandler : IRequestHandler<GetBasketQuery, BasketDto>
{
    private readonly BasketDbContext _context;
    private readonly IMapper _mapper;

    public GetBasketQueryHandler(BasketDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<BasketDto> Handle(
        GetBasketQuery request,
        CancellationToken cancellationToken)
    {
        var basket = await _context.Baskets
                         .Include(b => b.Items)
                         .FirstOrDefaultAsync(b => b.UserId == request.UserId, cancellationToken);

        if (basket == null)
        {
            basket = new Basket.Baskets.Models.Basket { UserId = request.UserId };
            _context.Baskets.Add(basket);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return _mapper.Map<BasketDto>(basket);
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