using Ardalis.GuardClauses;
using BuildingBlocks.Contracts.EventBus.Messages;
using Catalog.Data;
using Catalog.Products.Exceptions;
using Catalog.Products.Models;
using MapsterMapper;
using MassTransit;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Catalog.Products.Features.AddProduct;

public class AddProductMongoHandler : IConsumer<ProductAddedIntegrationEvent>
{
    private readonly CatalogReadDbContext _catalogReadDbContext;
    private readonly IMapper _mapper;

    public AddProductMongoHandler(
        IMapper mapper,
        CatalogReadDbContext catalogReadDbContext
    )
    {
        _mapper = mapper;
        _catalogReadDbContext = catalogReadDbContext;
    }

    public async Task Consume(ConsumeContext<ProductAddedIntegrationEvent> context)
    {
        Guard.Against.Null(context, nameof(context));

        var productReadModel = _mapper.Map<ProductReadModel>(context.Message);

        var product = await _catalogReadDbContext.Product.AsQueryable()
                          .FirstOrDefaultAsync(x => x.Id == productReadModel.Id && !x.IsDeleted, context.CancellationToken);

        if (product is not null)
        {
            throw new ProductAlreadyExistException();
        }

        await _catalogReadDbContext.Product.InsertOneAsync(productReadModel, cancellationToken: context.CancellationToken);
    }
}