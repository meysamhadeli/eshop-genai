using BuildingBlocks.Web;
using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Order.Data;
using Order.Orders.Dtos;
using Order.Orders.Exceptions;

namespace Order.Orders.Features;

public record GetOrderById(Guid OrderId) : IRequest<OrderDto>;

public class GetOrderByIdHandler : IRequestHandler<GetOrderById, OrderDto>
{
    private readonly OrderDbContext _context;
    private readonly IMapper _mapper;

    public GetOrderByIdHandler(OrderDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<OrderDto> Handle(GetOrderById request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
                        .Include(o => o.Items)
                        .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
        {
            throw new OrderNotFoundException();
        }

        return _mapper.Map<OrderDto>(order);
    }
}

public class GetOrderByIdEndpoints : IMinimalEndpoint
{
    public IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapGet($"{EndpointConfig.BaseApiPath}/order/{{id}}", async (
                                                                          Guid id,
                                                                          IMediator mediator,
                                                                          CancellationToken cancellationToken) =>
                                                                      {
                                                                          var result = await mediator.Send(new GetOrderById(id), cancellationToken);
                                                                          return Results.Ok(result);
                                                                      })
            .WithName("GetOrderById")
            .WithApiVersionSet(builder.NewApiVersionSet("Order").Build())
            .Produces<OrderDto>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get Order By Id")
            .WithDescription("Get order details by ID")
            .WithOpenApi()
            .HasApiVersion(1.0);

        return builder;
    }
}