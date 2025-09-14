using Ardalis.GuardClauses;
using BuildingBlocks.AI.Qdrant;
using BuildingBlocks.Contracts.EventBus.Messages;
using Catalog.Products.Exceptions;
using Catalog.Products.Models;
using MapsterMapper;
using MassTransit;

namespace Catalog.Products.Features.AddProduct;

public class AddProductQdrantHandler : IConsumer<ProductAddedIntegrationEvent>
{
    private readonly IQdrantRepository<ProductReadModel> _qdrantRepository;
    private readonly IMapper _mapper;

    public AddProductQdrantHandler(
        IQdrantRepository<ProductReadModel> qdrantRepository,
        IMapper mapper
    )
    {
        _qdrantRepository = qdrantRepository;
        _mapper = mapper;
    }

    public async Task Consume(ConsumeContext<ProductAddedIntegrationEvent> context)
    {
        Guard.Against.Null(context, nameof(context));

        var productReadModel = _mapper.Map<ProductReadModel>(context.Message);

        var product = await _qdrantRepository.GetByIdAsync(productReadModel.Id, cancellationToken: context.CancellationToken);

        if (product is not null)
        {
            throw new ProductAlreadyExistException();
        }

        await _qdrantRepository.IndexAsync(product, context.CancellationToken);
    }
}