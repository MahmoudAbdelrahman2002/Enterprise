using Enterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enterprise.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Sku).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Price).HasPrecision(18, 2);
        builder.Property(p => p.Status).HasConversion<int>().IsRequired();
        builder.Property(p => p.ImageUrl).HasMaxLength(2048);

        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => new { p.CategoryId, p.Sku })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Translations)
            .WithOne()
            .HasForeignKey(t => t.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Translations)
            .HasField("_translations")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude();
    }
}
