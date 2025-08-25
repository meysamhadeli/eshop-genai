// Update CatalogDataSeeder.cs

using BuildingBlocks.EFCore;
using BuildingBlocks.SemanticSearch;
using Catalog.Data;
using Catalog.Products.Dtos;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Flight.Data.Seed;

public class CatalogDataSeeder : IDataSeeder
{
    private readonly CatalogDbContext _catalogDbContext;
    private readonly IMapper _mapper;
    private readonly ISemanticSearchService _semanticSearchService;
    private readonly ILogger<CatalogDataSeeder> _logger;

    public CatalogDataSeeder(
        CatalogDbContext catalogDbContext,
        IMapper mapper,
        ISemanticSearchService semanticSearchService,
        ILogger<CatalogDataSeeder> logger)
    {
        _catalogDbContext = catalogDbContext;
        _mapper = mapper;
        _semanticSearchService = semanticSearchService;
        _logger = logger;
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
        if (!await _catalogDbContext.Products.AnyAsync())
        {
            await _catalogDbContext.Products.AddRangeAsync(InitialData.Products);
            await _catalogDbContext.SaveChangesAsync();

            await IndexAllProductsAsync();
        }
    }

    private async Task IndexAllProductsAsync()
    {
        var products = await _catalogDbContext.Products.ToListAsync();
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