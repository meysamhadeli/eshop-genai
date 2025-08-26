using Basket;
using BuildingBlocks.Core;
using BuildingBlocks.Core.Event;
using BuildingBlocks.Web;
using Catalog;
using MapsterMapper;
using MediatR;
using Order.Data;
using Order.Orders.Dtos;
using Order.Orders.Enums;
using Order.Orders.Exceptions;
using Order.Orders.Models;

namespace Order.Orders.Features;
public record CreateOrder(string UserId, string ShippingAddress) : IRequest<OrderDto>;

public record OrderCreatedIntegrationEvent(Guid Id, string UserId, OrderStatus Status, decimal TotalAmount, string ShippingAddress,
                                       DateTime OrderDate, ICollection<OrderItem> Items, bool IsDeleted) : IIntegrationEvent;


public class CreateOrderCommandHandler : IRequestHandler<CreateOrder, OrderDto>
{
    private readonly OrderDbContext _orderDbContext;
    private readonly BasketGrpcService.BasketGrpcServiceClient _basketGrpcServiceClient;
    private readonly CatalogGrpcService.CatalogGrpcServiceClient _catalogGrpcServiceClient;
    private readonly IMapper _mapper;
    private readonly IIntegrationEventCollector _integrationEventCollector;

    public CreateOrderCommandHandler(
        OrderDbContext orderDbContext,
        BasketGrpcService.BasketGrpcServiceClient basketGrpcServiceClient,
        CatalogGrpcService.CatalogGrpcServiceClient catalogGrpcServiceClient,
        IMapper mapper,
        IIntegrationEventCollector integrationEventCollector)
    {
        _orderDbContext = orderDbContext;
        _basketGrpcServiceClient = basketGrpcServiceClient;
        _catalogGrpcServiceClient = catalogGrpcServiceClient;
        _mapper = mapper;
        _integrationEventCollector = integrationEventCollector;
    }

    public async Task<OrderDto> Handle(CreateOrder request, CancellationToken cancellationToken)
    {
        var basketResponse = await _basketGrpcServiceClient.GetBasketAsync(new GetBasketRequest { UserId = request.UserId }, cancellationToken: cancellationToken);

        if (basketResponse?.Items == null || !basketResponse.Items.Any())
        {
            throw new EmptyBasketException();
        }

        var orderItems = new List<OrderItem>();
        decimal totalAmount = 0;

        foreach (var basketItem in basketResponse.Items)
        {
            var productResponse = await _catalogGrpcServiceClient.GetProductByIdAsync(
                new GetProductByIdRequest { Id = basketItem.ProductId },
                cancellationToken: cancellationToken);

            if (productResponse?.ProductDto == null)
            {
                throw new ProductNotFoundException();
            }

            var orderItem = new OrderItem
            {
                ProductId = new Guid(productResponse.ProductDto.Id),
                ProductName = productResponse.ProductDto.Name,
                UnitPrice = (decimal)productResponse.ProductDto.Price,
                Quantity = basketItem.Quantity,
                ImageUrl = productResponse.ProductDto.ImageUrl
            };

            orderItems.Add(orderItem);
            totalAmount += orderItem.TotalPrice;
        }

        var order = new Models.Order
        {
            UserId = request.UserId,
            Status = OrderStatus.Pending,
            TotalAmount = totalAmount,
            ShippingAddress = request.ShippingAddress,
            Items = orderItems,
            OrderDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _orderDbContext.Orders.Add(order);
        
        _integrationEventCollector.AddEvent(new OrderCreatedIntegrationEvent(order.Id, order.UserId, order.Status, order.TotalAmount, order.ShippingAddress, order.OrderDate, order.Items, order.IsDeleted));

        await _basketGrpcServiceClient.ClearBasketAsync(new ClearBasketRequest { UserId = request.UserId }, cancellationToken: cancellationToken);

        return _mapper.Map<OrderDto>(order);
    }
}

public class CreateOrderEndpoints : IMinimalEndpoint
{
    public IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPost($"{EndpointConfig.BaseApiPath}/order", async (
            CreateOrder command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken);
            return Results.Created($"/order/{result.Id}", result);
        })
        .WithName("CreateOrder")
        .WithApiVersionSet(builder.NewApiVersionSet("Order").Build())
        .Produces<OrderDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Create Order")
        .WithDescription("Create a new order from basket items")
        .WithOpenApi()
        .HasApiVersion(1.0);

        return builder;
    }
}