using Ardalis.GuardClauses;
using BuildingBlocks.Contracts.EventBus.Messages;
using Catalog.Data;
using Catalog.Products.Exceptions;
using Catalog.Products.Models;
using Mapster;
using MapsterMapper;
using MassTransit;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Catalog.Products.Features.AddProduct;

public class AddProductMongoHandler : IConsumer<ProductAddedIntegrationEvent>
{
    private readonly CatalogReadDbContext _catalogReadDbContext;

    public AddProductMongoHandler(
        CatalogReadDbContext catalogReadDbContext
    )
    {
        _catalogReadDbContext = catalogReadDbContext;
    }

    public async Task Consume(ConsumeContext<ProductAddedIntegrationEvent> context)
    {
        Guard.Against.Null(context, nameof(context));

        var productReadModel = context.Message.Adapt<ProductMongoModel>();

        var product = await _catalogReadDbContext.Product.AsQueryable()
                          .FirstOrDefaultAsync(x => x.Id == productReadModel.Id && !x.IsDeleted, context.CancellationToken);

        if (product is not null)
        {
            throw new ProductAlreadyExistException();
        }

        await _catalogReadDbContext.Product.InsertOneAsync(productReadModel, cancellationToken: context.CancellationToken);
    }
}