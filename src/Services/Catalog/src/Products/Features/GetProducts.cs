using BuildingBlocks.Core.Pagination;
using BuildingBlocks.Web;
using Catalog.Data;
using Catalog.Products.Dtos;
using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Products.Features;

public record GetProducts(
    int PageNumber = 1,
    int PageSize = 10,
    string SearchTerm = "") : IRequest<PageList<ProductDto>>;

public class GetProductsQueryHandler : IRequestHandler<GetProducts, PageList<ProductDto>>
{
    private readonly CatalogDbContext _context;
    private readonly IMapper _mapper;

    public GetProductsQueryHandler(CatalogDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PageList<ProductDto>> Handle(
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