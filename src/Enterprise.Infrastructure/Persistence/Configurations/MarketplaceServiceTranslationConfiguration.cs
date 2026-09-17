using Enterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enterprise.Infrastructure.Persistence.Configurations;

public sealed class MarketplaceServiceTranslationConfiguration : IEntityTypeConfiguration<MarketplaceServiceTranslation>
{
    public void Configure(EntityTypeBuilder<MarketplaceServiceTranslation> builder)
    {
        builder.ToTable("MarketplaceServiceTranslations");
        builder.HasKey(t => new { t.MarketplaceServiceId, t.LanguageCode });

        builder.Property(t => t.LanguageCode).IsRequired().HasMaxLength(5);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Description).HasMaxLength(2000);

        builder.HasIndex(t => new { t.LanguageCode, t.Name });
    }
}
