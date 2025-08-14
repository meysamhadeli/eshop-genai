using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Basket.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BasketDbContext>
    {
        public BasketDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<BasketDbContext>();

            builder.UseNpgsql("Server=localhost;Port=5432;Database=basket;User Id=postgres;Password=postgres;Include Error Detail=true")
                .UseSnakeCaseNamingConvention();
            return new BasketDbContext(builder.Options);
        }
    }
}