using BuildingBlocks.Mongo;
using Catalog.Products.Models;
using Humanizer;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Catalog.Data;

public class CatalogReadDbContext : MongoDbContext
{
    public CatalogReadDbContext(IOptions<MongoOptions> options) : base(options)
    {
        Product = GetCollection<ProductReadModel>(nameof(Product).Underscore());
    }

    public IMongoCollection<ProductReadModel> Product { get; }
}