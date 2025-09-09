using BuildingBlocks.AI.SemanticSearch;
using BuildingBlocks.AI.SemanticSearch.Models;
using BuildingBlocks.Web;
using MediatR;

namespace Catalog.Products.Features.TrackUserActivity;

public record TrackUserActivity(
    string UserId,
    string ItemId,
    string ActivityType = ActivityTypes.View,
    Dictionary<string, object>? Metadata = null,
    ActivityContext? Context = null) : IRequest;

public class TrackUserActivityHandler : IRequestHandler<TrackUserActivity>
{
    private readonly IRecommendationService _recommendationService;
    private readonly ILogger<TrackUserActivityHandler> _logger;

    public TrackUserActivityHandler(
        IRecommendationService recommendationService,
        ILogger<TrackUserActivityHandler> logger)
    {
        _recommendationService = recommendationService;
        _logger = logger;
    }

    public async Task Handle(TrackUserActivity request, CancellationToken cancellationToken)
    {
            var userActivity = new UserActivity
            {
                UserId = request.UserId,
                ItemId = request.ItemId,
                ActivityType = request.ActivityType,
                Metadata = request.Metadata ?? new Dictionary<string, object>(),
                Context = request.Context
            };

            await _recommendationService.TrackUserActivityAsync(userActivity, cancellationToken);
            
            _logger.LogInformation("Tracked {ActivityType} activity for user {UserId} on item {ItemId}", request.ActivityType, request.UserId, request.ItemId);
    }
}

public class TrackUserActivityEndpoints : IMinimalEndpoint
{
    public IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPost($"{EndpointConfig.BaseApiPath}/recommendations/activity", async (
            TrackUserActivity command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            await mediator.Send(command, cancellationToken);
            return Results.Ok(new { Message = "Activity tracked successfully" });
        })
        .WithName("TrackUserActivity")
        .WithApiVersionSet(builder.NewApiVersionSet("Recommendations").Build())
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Track User Activity")
        .WithDescription("Track user activity for recommendations")
        .WithOpenApi()
        .HasApiVersion(1.0);

        return builder;
    }
}