using BuildingBlocks.Core.Pagination;
using BuildingBlocks.Web;
using Catalog.Data;
using Catalog.Products.Dtos;
using Catalog.Products.Exceptions;
using Mapster;
using MapsterMapper;
using MediatR;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Catalog.Products.Features.GetproductById;

public record GetProductById(Guid ProductId) : IRequest<ProductDto>;

public class GetProductByIdHandler : IRequestHandler<GetProductById, ProductDto>
{
    private readonly CatalogReadDbContext _catalogReadDbContext;

    public GetProductByIdHandler(CatalogDbContext context, IMapper mapper, CatalogReadDbContext catalogReadDbContext)
    {
        _catalogReadDbContext = catalogReadDbContext;
    }

    public async Task<ProductDto> Handle(GetProductById request, CancellationToken cancellationToken)
    {
        var product = await _catalogReadDbContext.Product.AsQueryable().FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);

        if (product == null)
        {
            throw new ProductNotFoundException();
        }

        var productDto = product.Adapt<ProductDto>();

        return productDto;
    }
}


public class GetProductByIdEndpoints : IMinimalEndpoint
{
    public IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapGet($"{EndpointConfig.BaseApiPath}/product/{{id}}", async (Guid id,
                                                                              IMediator mediator,
                                                                              CancellationToken cancellationToken) =>
                                                                {
                                                                    var result = await mediator.Send(new GetProductById(id), cancellationToken);
                                                                    return Results.Ok(result);
                                                                })
            .WithName("GetProductById")
            .WithApiVersionSet(builder.NewApiVersionSet("Product").Build())
            .Produces<PageList<ProductDto>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithSummary("Get Product By Id")
            .WithDescription("Get Product By Id")
            .WithOpenApi()
            .HasApiVersion(1.0);

        return builder;

    }
}