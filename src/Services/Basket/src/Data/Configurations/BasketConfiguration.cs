using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Basket.Data.Configurations;

public class BasketConfiguration : IEntityTypeConfiguration<Basket.Baskets.Models.Basket>
{
    public void Configure(EntityTypeBuilder<Basket.Baskets.Models.Basket> builder)
    {
        builder.ToTable(nameof(Basket));
        
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id)
            .ValueGeneratedOnAdd();
            
        builder.Property(b => b.UserId)
            .IsRequired()
            .HasMaxLength(256);
            
        // Relationships
        builder.HasMany(b => b.Items)
            .WithOne()
            .HasForeignKey(i => i.BasketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}