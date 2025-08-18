using BuildingBlocks.EFCore;
using BuildingBlocks.Web;
using Microsoft.EntityFrameworkCore;

namespace Basket.Data;

public sealed class BasketDbContext : AppDbContextBase
{
    public BasketDbContext(DbContextOptions<BasketDbContext> options, ICurrentUserProvider? currentUserProvider = null,
                           ILogger<BasketDbContext>? logger = null) : base(
        options, currentUserProvider, logger)
    {
    }

    public DbSet<Basket.Baskets.Models.Basket> Baskets => Set<Basket.Baskets.Models.Basket>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(Program).Assembly);
        builder.FilterSoftDeletedProperties();
        builder.ToSnakeCaseTables();
    }
}