using BuildingBlocks.Contracts.EventBus.Messages;
using BuildingBlocks.Core;
using BuildingBlocks.Core.Event;
using BuildingBlocks.Web;
using Catalog.Data;
using Catalog.Products.Dtos;
using Catalog.Products.Models;
using Mapster;
using MapsterMapper;
using MediatR;

namespace Catalog.Products.Features.AddProduct;

public record AddProduct(
    string Name,
    string Description,
    decimal Price,
    string ImageUrl) : IRequest<ProductDto>;


public class AddProductCommandHandler : IRequestHandler<AddProduct, ProductDto>
{
    private readonly CatalogDbContext _context;
    private readonly IIntegrationEventCollector _integrationEventCollector;

    public AddProductCommandHandler(CatalogDbContext context, IMapper mapper, IIntegrationEventCollector integrationEventCollector)
    {
        _context = context;
        _integrationEventCollector = integrationEventCollector;
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

        _integrationEventCollector.AddIntegrationEvent(new ProductAddedIntegrationEvent(product.Id, product.Name, product.Description, product.Price, product.ImageUrl, false));

        return product.Adapt<ProductDto>();
    }
}

public class AddProductsEndpoints : IMinimalEndpoint
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