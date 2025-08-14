using Basket.Baskets.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Basket.Data.Configurations;

public class BasketItemsConfiguration : IEntityTypeConfiguration<BasketItems>
{
    public void Configure(EntityTypeBuilder<BasketItems> builder)
    {
        builder.ToTable(nameof(BasketItems));
        
        builder.HasKey(i => new { i.BasketId, i.ProductId });
        
        builder.Property(i => i.Quantity)
            .IsRequired()
            .HasDefaultValue(1);
    }
}