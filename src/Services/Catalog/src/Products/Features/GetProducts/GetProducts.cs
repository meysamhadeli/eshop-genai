using BuildingBlocks.AI.Qdrant;
using BuildingBlocks.AI.SemanticSearch;
using BuildingBlocks.Core.Pagination;
using BuildingBlocks.Web;
using Catalog.Data;
using Catalog.Products.Dtos;
using Catalog.Products.Models;
using MapsterMapper;
using MediatR;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Catalog.Products.Features.GetProducts;

public record GetProducts(
    int PageNumber = 1,
    int PageSize = 10,
    string SearchTerm = "",
    bool UseSemanticSearch = true) : IRequest<PageList<ProductDto>>;

public class GetProductsHandler : IRequestHandler<GetProducts, PageList<ProductDto>>
{
    private readonly IMapper _mapper;
    private readonly CatalogReadDbContext _catalogReadDbContext;
    private readonly ISemanticSearchService _semanticSearchService;

    public GetProductsHandler(
        IMapper mapper,
        CatalogReadDbContext catalogReadDbContext,
        ISemanticSearchService semanticSearchService)
    {
        _mapper = mapper;
        _catalogReadDbContext = catalogReadDbContext;
        _semanticSearchService = semanticSearchService;
    }

    public async Task<PageList<ProductDto>> Handle(
        GetProducts request,
        CancellationToken cancellationToken)
    {
        if (request.UseSemanticSearch && !string.IsNullOrEmpty(request.SearchTerm))
        {
            var semanticResults = await _semanticSearchService.SemanticSearchAsync<ProductReadModel>(
                request.SearchTerm,
                maxResults: request.PageSize,
                cancellationToken: cancellationToken);

            if (semanticResults.Results.Any())
            {
                var productDtos = _mapper.Map<IReadOnlyList<ProductDto>>(semanticResults.Results);

                return PageList<ProductDto>.Create(
                    productDtos,
                    request.PageNumber,
                    request.PageSize,
                    semanticResults.TotalCounts,
                    semanticResults.HasExactMatches,
                    semanticResults.Explanation);
            }
        }

        // Fallback to regular search
        return await ExecuteRegularSearchAsync(request, cancellationToken);
    }

    private async Task<PageList<ProductDto>> ExecuteRegularSearchAsync(
        GetProducts request,
        CancellationToken cancellationToken)
    {
        var query = _catalogReadDbContext.Product.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(p =>
                p.Name.Contains(request.SearchTerm) ||
                p.Description.Contains(request.SearchTerm));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var products = await query
            .OrderBy(p => p.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var productDtos = _mapper.Map<List<ProductDto>>(products);

        return PageList<ProductDto>.Create(
            productDtos,
            request.PageNumber,
            request.PageSize,
            totalCount);
    }
}

public class GetProductsEndpoints : IMinimalEndpoint
{
    public IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapGet($"{EndpointConfig.BaseApiPath}/product", async (
                                                                    [AsParameters] GetProducts query,
                                                                    IMediator mediator,
                                                                    CancellationToken cancellationToken) =>
                                                                {
                                                                    var result = await mediator.Send(query, cancellationToken);
                                                                    return Results.Ok(result);
                                                                })
            .WithName("GetProducts")
            .WithApiVersionSet(builder.NewApiVersionSet("Product").Build())
            .Produces<PageList<ProductDto>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithSummary("Get Products")
            .WithDescription("Get paginated list of products with optional search term")
            .WithOpenApi()
            .HasApiVersion(1.0);

        return builder;

    }
}