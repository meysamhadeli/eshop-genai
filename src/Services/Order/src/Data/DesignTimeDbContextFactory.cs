using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Order.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<OrderDbContext>
    {
        public OrderDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<OrderDbContext>();

            builder.UseNpgsql("Server=localhost;Port=5432;Database=order;User Id=postgres;Password=postgres;Include Error Detail=true")
                .UseSnakeCaseNamingConvention();
            return new OrderDbContext(builder.Options);
        }
    }
}