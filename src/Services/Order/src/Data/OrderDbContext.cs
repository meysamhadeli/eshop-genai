using BuildingBlocks.EFCore;
using BuildingBlocks.Web;
using Microsoft.EntityFrameworkCore;

namespace Order.Data;

public sealed class OrderDbContext : AppDbContextBase
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options, ICurrentUserProvider? currentUserProvider = null,
        ILogger<OrderDbContext>? logger = null) : base(
        options, currentUserProvider, logger)
    {
    }

    public DbSet<Order.Orders.Models.Order> Orders => Set<Order.Orders.Models.Order>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(Program).Assembly);
        builder.FilterSoftDeletedProperties();
        builder.ToSnakeCaseTables();
    }
}