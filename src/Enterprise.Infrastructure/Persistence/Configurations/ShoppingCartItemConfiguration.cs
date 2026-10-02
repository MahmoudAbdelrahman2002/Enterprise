using Enterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enterprise.Infrastructure.Persistence.Configurations;

public sealed class ShoppingCartItemConfiguration : IEntityTypeConfiguration<ShoppingCartItem>
{
    public void Configure(EntityTypeBuilder<ShoppingCartItem> builder)
    {
        builder.ToTable("ShoppingCartItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Quantity).IsRequired();

        builder.HasIndex(i => new { i.ShoppingCartId, i.ProductId })
            .IsUnique();

        builder.HasIndex(i => i.ProductId);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
