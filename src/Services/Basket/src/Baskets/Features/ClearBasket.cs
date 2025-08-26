using Basket.Infrastructure.Redis;
using BuildingBlocks.Core;
using BuildingBlocks.Core.Event;
using BuildingBlocks.Web;
using MediatR;

namespace Basket.Baskets.Features;

public record ClearBasket(string UserId) : IRequest<bool>;

public record ClearBasketItemIntegrationEvent(string UserId, bool IsCleared) : IIntegrationEvent;

public class ClearBasketHandler : IRequestHandler<ClearBasket, bool>
{
    private readonly IBasketRedisService _basketRedisService;
    private readonly ILogger<ClearBasketHandler> _logger;
    private readonly IEventDispatcher _eventDispatcher;

    public ClearBasketHandler(
        IBasketRedisService basketRedisService,
        ILogger<ClearBasketHandler> logger,
        IEventDispatcher eventDispatcher)
    {
        _basketRedisService = basketRedisService;
        _logger = logger;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<bool> Handle(ClearBasket request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Clearing basket for user: {UserId}", request.UserId);

        var isCleared = await _basketRedisService.ClearBasketAsync(request.UserId, cancellationToken);

        await _eventDispatcher.SendAsync(new ClearBasketItemIntegrationEvent(request.UserId, isCleared), cancellationToken: cancellationToken);

        return isCleared;
    }
}

public class ClearBasketEndpoints : IMinimalEndpoint
{
    public IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapDelete($"{EndpointConfig.BaseApiPath}/basket", async (
            [AsParameters] ClearBasket command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken);

            if (result)
            {
                return Results.Ok(new { Message = "Basket cleared successfully" });
            }
            else
            {
                return Results.Problem("Failed to clear basket", statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("ClearBasket")
        .WithApiVersionSet(builder.NewApiVersionSet("Basket").Build())
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithSummary("Clear Basket")
        .WithDescription("Clear all items from the user's basket")
        .WithOpenApi()
        .HasApiVersion(1.0);

        return builder;
    }
}