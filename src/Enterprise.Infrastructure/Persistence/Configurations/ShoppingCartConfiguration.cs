using Enterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enterprise.Infrastructure.Persistence.Configurations;

public sealed class ShoppingCartConfiguration : IEntityTypeConfiguration<ShoppingCart>
{
    public void Configure(EntityTypeBuilder<ShoppingCart> builder)
    {
        builder.ToTable("ShoppingCarts");
        builder.HasKey(c => c.Id);

        builder.HasIndex(c => new { c.UserId, c.ProviderId })
            .IsUnique();

        builder.HasIndex(c => c.ProviderId);
        builder.HasIndex(c => c.UserId);

        builder.HasOne(c => c.Provider)
            .WithMany()
            .HasForeignKey(c => c.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Items)
            .WithOne(i => i.ShoppingCart)
            .HasForeignKey(i => i.ShoppingCartId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
