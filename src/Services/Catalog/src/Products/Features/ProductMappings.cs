using BuildingBlocks.Contracts.EventBus.Messages;
using Catalog.Products.Models;
using Mapster;
using MassTransit;

namespace Flight.Flights.Features;

public class ProductMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ProductAddedIntegrationEvent, ProductReadModel>()
            .Map(d => d.Id, s => s.Id)
            .Map(d => d.Description, s => s.Description)
            .Map(d => d.Name, s => s.Name)
            .Map(d => d.ImageUrl, s => s.ImageUrl)
            .Map(d => d.Price, s => s.Price)
            .Map(d => d.CreatedAt, s => DateTime.UtcNow)
            .Map(d => d.IsDeleted, s => s.IsDeleted);
    }
}