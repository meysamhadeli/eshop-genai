using BuildingBlocks.Core.Pagination;
using BuildingBlocks.SemanticSearch;
using BuildingBlocks.Web;
using Catalog.Data;
using Catalog.Products.Dtos;
using Catalog.Products.Models;
using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Products.Features;

public record GetProducts(
    int PageNumber = 1,
    int PageSize = 10,
    string SearchTerm = "",
    bool UseSemanticSearch = true) : IRequest<PageList<ProductDto>>;

public class GetProductsHandler : IRequestHandler<GetProducts, PageList<ProductDto>>
{
    private readonly CatalogDbContext _context;
    private readonly IMapper _mapper;
    private readonly ISemanticSearchService _semanticSearchService;

    public GetProductsHandler(
        CatalogDbContext context, 
        IMapper mapper,
        ISemanticSearchService semanticSearchService)
    {
        _context = context;
        _mapper = mapper;
        _semanticSearchService = semanticSearchService;
    }

    public async Task<PageList<ProductDto>> Handle(
        GetProducts request,
        CancellationToken cancellationToken)
    {
        if (request.UseSemanticSearch && !string.IsNullOrWhiteSpace(request.SearchTerm))
        {
                    var semanticResults = await _semanticSearchService.SemanticSearchAsync<Product, ProductDto>(
                        request.SearchTerm, 
                        maxResults: request.PageSize, 
                        cancellationToken: cancellationToken);

                    if (semanticResults.Any())
                    {
                        var productIds = semanticResults.Select(p => p.Id).ToList();
                        
                        var products = await _context.Products
                            .Where(p => productIds.Contains(p.Id))
                            .ToListAsync(cancellationToken);

                        var productDtos = _mapper.Map<List<ProductDto>>(products);
                        
                        var orderedResults = productIds
                            .Select(id => productDtos.FirstOrDefault(p => p.Id == id))
                            .Where(p => p != null)
                            .ToList();

                        return new PageList<ProductDto>(
                            orderedResults!, 
                            orderedResults.Count, 
                            request.PageNumber, 
                            request.PageSize);
                    }
        }
        
        // Fallback to regular search
        return await ExecuteRegularSearchAsync(request, cancellationToken);
    }

    private async Task<PageList<ProductDto>> ExecuteRegularSearchAsync(
        GetProducts request, 
        CancellationToken cancellationToken)
    {
        var query = _context.Products.AsQueryable();

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

        return new PageList<ProductDto>(
            productDtos, 
            totalCount, 
            request.PageNumber, 
            request.PageSize);
    }
}

public class GetProductsEndpoints: IMinimalEndpoint
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