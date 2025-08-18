using BuildingBlocks.Web;
using Catalog.Data;
using Catalog.Products.Dtos;
using Catalog.Products.Models;
using MapsterMapper;
using MediatR;

namespace Catalog.Products.Features;

public record AddProduct(
    string Name,
    string Description,
    decimal Price,
    string ImageUrl) : IRequest<ProductDto>;

public class AddProductCommandHandler : IRequestHandler<AddProduct, ProductDto>
{
    private readonly CatalogDbContext _context;
    private readonly IMapper _mapper;

    public AddProductCommandHandler(CatalogDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ProductDto> Handle(
        AddProduct request,
        CancellationToken cancellationToken)
    {
        var product = new Product
                      {
                          Name = request.Name,
                          Description = request.Description,
                          Price = request.Price,
                          ImageUrl = request.ImageUrl
                      };

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ProductDto>(product);
    }
}

public class AddProductsEndpoints: IMinimalEndpoint
{
    public IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPost($"{EndpointConfig.BaseApiPath}/product", async (
                                                                     AddProduct command,
                                                                     IMediator mediator,
                                                                     CancellationToken cancellationToken) =>
                                                                 {
                                                                     var result = await mediator.Send(command, cancellationToken);
                                                                     return Results.Created($"/api/products/{result.Id}", result);
                                                                 })
            .WithName("AddProduct")
            .WithApiVersionSet(builder.NewApiVersionSet("Product").Build())
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithSummary("Add Product")
            .WithDescription("Add new product")
            .WithOpenApi()
            .HasApiVersion(1.0);

        return builder;
    }
}