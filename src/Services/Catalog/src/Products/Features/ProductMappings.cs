using BuildingBlocks.Contracts.EventBus.Messages;
using Catalog.Products.Dtos;
using Catalog.Products.Models;
using Mapster;

namespace Catalog.Products.Features;

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

        config.NewConfig<Product, ProductReadModel>()
            .Map(d => d.Id, s => s.Id)
            .Map(d => d.Description, s => s.Description)
            .Map(d => d.Name, s => s.Name)
            .Map(d => d.ImageUrl, s => s.ImageUrl)
            .Map(d => d.Price, s => s.Price)
            .Map(d => d.CreatedAt, s => DateTime.UtcNow)
            .Map(d => d.LastModified, s => s.LastModified)
            .Map(d => d.CreatedBy, s => s.CreatedBy)
            .Map(d => d.LastModifiedBy, s => s.LastModifiedBy)
            .Map(d => d.IsDeleted, s => s.IsDeleted);

        config.NewConfig<ProductReadModel, ProductDto>()
            .Map(d => d.Id, s => s.Id)
            .Map(d => d.Description, s => s.Description)
            .Map(d => d.Name, s => s.Name)
            .Map(d => d.ImageUrl, s => s.ImageUrl)
            .Map(d => d.Price, s => s.Price)
            .Map(d => d.CreatedAt, s => DateTime.UtcNow)
            .Map(d => d.LastModified, s => s.LastModified);
    }
}