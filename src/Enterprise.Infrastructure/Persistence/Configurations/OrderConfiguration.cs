using Enterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enterprise.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.TotalAmount).HasPrecision(18, 2);
        builder.Property(o => o.Status).HasConversion<int>().IsRequired();
        builder.Property(o => o.Notes).HasMaxLength(1000);
        builder.Property(o => o.OrderDateUtc).IsRequired();
        builder.Property(o => o.StripeCheckoutSessionId).HasMaxLength(255);

        builder.HasIndex(o => o.StripeCheckoutSessionId)
            .IsUnique()
            .HasFilter("[StripeCheckoutSessionId] IS NOT NULL");

        builder.HasIndex(o => o.UserId);
        builder.HasIndex(o => o.ProviderId);
        builder.HasIndex(o => new { o.ProviderId, o.OrderDateUtc });

        builder.HasOne(o => o.Provider)
            .WithMany()
            .HasForeignKey(o => o.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.OrderItems)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
