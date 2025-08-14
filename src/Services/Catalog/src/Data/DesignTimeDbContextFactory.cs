using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Catalog.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
    {
        public CatalogDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<CatalogDbContext>();

            builder.UseNpgsql("Server=localhost;Port=5432;Database=catalog;User Id=postgres;Password=postgres;Include Error Detail=true")
                .UseSnakeCaseNamingConvention();
            return new CatalogDbContext(builder.Options);
        }
    }
}