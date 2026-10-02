using Enterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enterprise.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProductName).IsRequired().HasMaxLength(256);
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.Quantity).IsRequired();

        builder.HasIndex(i => i.OrderId);
        builder.HasIndex(i => i.ProductId);

        // Snapshot row keeps ProductId for reference; product soft-delete must not remove history.
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
