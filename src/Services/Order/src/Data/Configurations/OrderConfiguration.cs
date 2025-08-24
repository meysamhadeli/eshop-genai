using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Order.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Orders.Models.Order>
{
    public void Configure(EntityTypeBuilder<Orders.Models.Order> builder)
    {
        builder.ToTable(nameof(Order));
        
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .ValueGeneratedOnAdd();
        
        builder.Property(o => o.UserId)
            .IsRequired()
            .HasMaxLength(255);
        
        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<string>() // Store enum as string in database
            .HasMaxLength(20);
        
        builder.Property(o => o.TotalAmount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");
        
        builder.Property(o => o.ShippingAddress)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(o => o.OrderDate)
            .IsRequired();
        
        builder.Property(r => r.Version).IsConcurrencyToken();
        
        // Configure one-to-many relationship with OrderItems
        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(x=> x.OrderId) // Shadow property for foreign key
            .OnDelete(DeleteBehavior.Cascade); // Delete items when order is deleted
        
        builder.HasIndex(o => o.UserId);
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.OrderDate);
    }
}