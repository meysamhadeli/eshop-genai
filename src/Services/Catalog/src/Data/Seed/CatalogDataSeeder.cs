// Update CatalogDataSeeder.cs

using BuildingBlocks.EFCore;
using BuildingBlocks.SemanticSearch;
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
    private readonly ISemanticSearchService _semanticSearchService;
    private readonly ILogger<CatalogDataSeeder> _logger;
    private readonly CatalogReadDbContext _catalogReadDbContext;

    public CatalogDataSeeder(
        CatalogDbContext catalogDbContext,
        IMapper mapper,
        ISemanticSearchService semanticSearchService,
        ILogger<CatalogDataSeeder> logger,
        CatalogReadDbContext catalogReadDbContext)
    {
        _catalogDbContext = catalogDbContext;
        _mapper = mapper;
        _semanticSearchService = semanticSearchService;
        _logger = logger;
        _catalogReadDbContext = catalogReadDbContext;
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

            await SeedMongoProducts();

            await SeedSemanticSearchProductsAsync();
        }
    }

    private async Task SeedMongoProducts()
    {
        if (!await MongoQueryable.AnyAsync(_catalogReadDbContext.Product.AsQueryable()))
        {
            await _catalogReadDbContext.Product.InsertManyAsync(_mapper.Map<List<ProductReadModel>>(InitialData.Products));
        }
    }

    private async Task SeedSemanticSearchProductsAsync()
    {
        var products = await EntityFrameworkQueryableExtensions.ToListAsync(_catalogDbContext.Products);
        var productDtos = _mapper.Map<List<ProductDto>>(products);

        foreach (var productDto in productDtos)
        {
            try
            {
                await _semanticSearchService.IndexAsync(productDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to index product {ProductDtoId}: {ExMessage}", productDto.Id, ex.Message);
            }
        }
    }
}