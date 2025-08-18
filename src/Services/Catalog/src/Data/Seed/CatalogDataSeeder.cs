using BuildingBlocks.EFCore;
using Catalog.Data;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Flight.Data.Seed;

public class CatalogDataSeeder(
    CatalogDbContext catalogDbContext,
    IMapper mapper
) : IDataSeeder
{
    public async Task SeedAllAsync()
    {
        var pendingMigrations = await catalogDbContext.Database.GetPendingMigrationsAsync();

        if (!pendingMigrations.Any())
        {
            await SeedProductAsync();
        }
    }

    private async Task SeedProductAsync()
    {
        if (!await catalogDbContext.Products.AnyAsync())
        {
            await catalogDbContext.Products.AddRangeAsync(InitialData.Products);
            await catalogDbContext.SaveChangesAsync();
        }
    }
}