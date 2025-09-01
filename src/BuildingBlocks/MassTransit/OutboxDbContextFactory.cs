using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BuildingBlocks.MassTransit;

public class OutboxDbContextFactory : IDesignTimeDbContextFactory<OutboxDbContext>
{
    public OutboxDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OutboxDbContext>();
        optionsBuilder.UseNpgsql("Server=localhost;Port=5432;Database=outbox_db;User Id=postgres;Password=postgres;Include Error Detail=true");

        return new OutboxDbContext(optionsBuilder.Options);
    }
}