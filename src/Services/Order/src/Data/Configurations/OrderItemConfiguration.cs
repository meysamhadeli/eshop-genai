using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Order.Orders.Models;

namespace Order.Data.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable(nameof(OrderItem));
        
        // Composite primary key (OrderId + ProductId)
        builder.HasKey(oi => new { oi.OrderId, oi.ProductId });
        
        builder.Property(oi => oi.OrderId)
            .IsRequired();
        
        builder.Property(oi => oi.ProductId)
            .IsRequired();
        
        builder.Property(oi => oi.ProductName)
            .IsRequired()
            .HasMaxLength(255);
        
        builder.Property(oi => oi.UnitPrice)
            .IsRequired()
            .HasColumnType("decimal(18,2)");
        
        builder.Property(oi => oi.Quantity)
            .IsRequired();
        
        builder.Property(oi => oi.ImageUrl)
            .IsRequired(false) 
            .HasMaxLength(500);
        
        builder.HasIndex(oi => oi.OrderId);
        builder.HasIndex(oi => oi.ProductId);
        
        // Configure relationship with Order (optional, but explicit)
        builder.HasOne<Orders.Models.Order>()
            .WithMany(o => o.Items)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}