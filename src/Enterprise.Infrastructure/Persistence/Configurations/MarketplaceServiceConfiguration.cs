using Enterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enterprise.Infrastructure.Persistence.Configurations;

public sealed class MarketplaceServiceConfiguration : IEntityTypeConfiguration<MarketplaceService>
{
    public void Configure(EntityTypeBuilder<MarketplaceService> builder)
    {
        builder.ToTable("MarketplaceServices");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Code).IsRequired().HasMaxLength(100);
        builder.Property(s => s.DisplayOrder).HasDefaultValue(0);
        builder.Property(s => s.IsActive).HasDefaultValue(true);

        builder.HasIndex(s => s.Code).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(s => s.DisplayOrder);
        builder.HasIndex(s => s.IsActive);

        builder.HasMany(s => s.Translations)
            .WithOne()
            .HasForeignKey(t => t.MarketplaceServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Translations)
            .HasField("_translations")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude();
    }
}
