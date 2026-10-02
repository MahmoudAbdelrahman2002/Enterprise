using Enterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enterprise.Infrastructure.Persistence.Configurations;

public sealed class ProductTranslationConfiguration : IEntityTypeConfiguration<ProductTranslation>
{
    public void Configure(EntityTypeBuilder<ProductTranslation> builder)
    {
        builder.ToTable("ProductTranslations");
        builder.HasKey(t => new { t.ProductId, t.LanguageCode });

        builder.Property(t => t.LanguageCode).IsRequired().HasMaxLength(5);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Description).HasMaxLength(2000);

        builder.HasIndex(t => new { t.LanguageCode, t.Name });
    }
}
