using BuildingBlocks.AI.Qdrant;
using BuildingBlocks.EFCore;
using Catalog.Products.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Catalog.Data.Seed;

public class CatalogDataSeeder : IDataSeeder
{
    private readonly CatalogDbContext _catalogDbContext;
    private readonly ILogger<CatalogDataSeeder> _logger;
    private readonly CatalogReadDbContext _catalogReadDbContext;
    private readonly IQdrantRepository<ProductQdrantModel> _qdrantRepository;

    public CatalogDataSeeder(
        CatalogDbContext catalogDbContext,
        ILogger<CatalogDataSeeder> logger,
        CatalogReadDbContext catalogReadDbContext,
        IQdrantRepository<ProductQdrantModel> qdrantRepository)
    {
        _catalogDbContext = catalogDbContext;
        _logger = logger;
        _catalogReadDbContext = catalogReadDbContext;
        _qdrantRepository = qdrantRepository;
    }

    public async Task SeedAllAsync()
    {
        var pendingMigrations = await _catalogDbContext.Database.GetPendingMigrationsAsync();

        if (!pendingMigrations.Any())
        {
            await SeedProductAsync();
        }
    }

    private async Task SeedProductAsync()
    {
        if (!await EntityFrameworkQueryableExtensions.AnyAsync(_catalogDbContext.Products))
        {
            await _catalogDbContext.Products.AddRangeAsync(InitialData.Products);
            await _catalogDbContext.SaveChangesAsync();

            await SeedMongoProductsAsync();
            await SeedQdrantProductsAsync();
        }
    }

    private async Task SeedMongoProductsAsync()
    {
        if (!await MongoQueryable.AnyAsync(_catalogReadDbContext.Product.AsQueryable()))
        {
            await _catalogReadDbContext.Product.InsertManyAsync(InitialData.Products.Adapt<List<ProductMongoModel>>());
        }
    }

    private async Task SeedQdrantProductsAsync()
    {
        var products = await EntityFrameworkQueryableExtensions.ToListAsync(_catalogDbContext.Products);
        var productQdrantModels = products.Adapt<List<ProductQdrantModel>>();

        foreach (var productQdrantModel in productQdrantModels)
        {
            try
            {
                await _qdrantRepository.IndexAsync(productQdrantModel);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to index product in Qdrant for {ProductReadModel}: {ExMessage}", productQdrantModel.Id, ex.Message);
            }
        }
    }
}