// Update CatalogDataSeeder.cs

using BuildingBlocks.AI.Qdrant;
using BuildingBlocks.AI.SemanticSearch;
using BuildingBlocks.EFCore;
using Catalog.Products.Dtos;
using Catalog.Products.Models;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Catalog.Data.Seed;

public class CatalogDataSeeder : IDataSeeder
{
    private readonly CatalogDbContext _catalogDbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<CatalogDataSeeder> _logger;
    private readonly CatalogReadDbContext _catalogReadDbContext;
    private readonly IQdrantRepository<ProductReadModel> _qdrantRepository;

    public CatalogDataSeeder(
        CatalogDbContext catalogDbContext,
        IMapper mapper,
        ILogger<CatalogDataSeeder> logger,
        CatalogReadDbContext catalogReadDbContext,
        IQdrantRepository<ProductReadModel> qdrantRepository)
    {
        _catalogDbContext = catalogDbContext;
        _mapper = mapper;
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
            await _catalogReadDbContext.Product.InsertManyAsync(_mapper.Map<List<ProductReadModel>>(InitialData.Products));
        }
    }

    private async Task SeedQdrantProductsAsync()
    {
        var products = await EntityFrameworkQueryableExtensions.ToListAsync(_catalogDbContext.Products);
        var productReadModels = _mapper.Map<List<ProductReadModel>>(products);

        foreach (var productReadModel in productReadModels)
        {
            try
            {
                await _qdrantRepository.IndexAsync(productReadModel);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to index product in Qdrant for {ProductReadModel}: {ExMessage}", productReadModel.Id, ex.Message);
            }
        }
    }
}