using Ardalis.GuardClauses;
using BuildingBlocks.AI.Qdrant;
using BuildingBlocks.Contracts.EventBus.Messages;
using Catalog.Products.Exceptions;
using Catalog.Products.Models;
using Mapster;
using MassTransit;

namespace Catalog.Products.Features.AddProduct;

public class AddProductQdrantHandler : IConsumer<ProductAddedIntegrationEvent>
{
    private readonly IQdrantRepository<ProductQdrantModel> _qdrantRepository;

    public AddProductQdrantHandler(
        IQdrantRepository<ProductQdrantModel> qdrantRepository
    )
    {
        _qdrantRepository = qdrantRepository;
    }

    public async Task Consume(ConsumeContext<ProductAddedIntegrationEvent> context)
    {
        Guard.Against.Null(context, nameof(context));

        var productQdrantModel = context.Message.Adapt<ProductQdrantModel>();

        var product = await _qdrantRepository.GetByIdAsync(productQdrantModel.Id, cancellationToken: context.CancellationToken);

        if (product is not null)
        {
            throw new ProductAlreadyExistException();
        }

        await _qdrantRepository.IndexAsync(product, context.CancellationToken);
    }
}