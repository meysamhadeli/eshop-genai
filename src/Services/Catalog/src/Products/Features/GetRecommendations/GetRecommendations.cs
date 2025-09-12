using BuildingBlocks.AI.Recommendation;
using BuildingBlocks.AI.SemanticSearch;
using BuildingBlocks.Core.Pagination;
using BuildingBlocks.Web;
using Catalog.Products.Dtos;
using MediatR;

namespace Catalog.Products.Features.GetRecommendationsByActivityType;

public record GetRecommendationsRequest(int PageNumber = 1, int PageSize = 10);

public record GetRecommendations(
    string UserId,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PageList<ProductDto>>;

public class GetRecommendationsByActivityTypeHandler : IRequestHandler<GetRecommendations, PageList<ProductDto>>
{
    private readonly IRecommendationService _recommendationService;

    public GetRecommendationsByActivityTypeHandler(
        IRecommendationService recommendationService,
        ILogger<GetRecommendationsByActivityTypeHandler> logger)
    {
        _recommendationService = recommendationService;
    }

    public async Task<PageList<ProductDto>> Handle(GetRecommendations request, CancellationToken cancellationToken)
    {
        var recommendations = await _recommendationService.GetRecommendationsAsync<ProductDto>(
            request.UserId,
            request.PageSize,
            cancellationToken: cancellationToken);

        return new PageList<ProductDto>(
            recommendations.Results.ToList(),
            recommendations.TotalCounts,
            request.PageNumber,
            request.PageSize);
    }
}

public class GetRecommendationsEndpoints : IMinimalEndpoint
{
    public IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapGet($"{EndpointConfig.BaseApiPath}/recommendations/{{userId}}", async (
            string userId,
            [AsParameters] GetRecommendationsRequest query,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetRecommendations(userId, query.PageNumber, query.PageSize), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetRecommendations")
        .WithApiVersionSet(builder.NewApiVersionSet("Recommendations").Build())
        .Produces<PageList<ProductDto>>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Get Recommendations")
        .WithDescription("Get personalized recommendations filtered by specific activity types")
        .WithOpenApi()
        .HasApiVersion(1.0);

        return builder;
    }
}