using Enterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enterprise.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.DisplayOrder).HasDefaultValue(0);
        builder.Property(c => c.IsActive).HasDefaultValue(true);

        builder.HasIndex(c => c.ProviderId);
        builder.HasIndex(c => c.DisplayOrder);
        builder.HasIndex(c => c.IsActive);

        builder.HasOne(c => c.Provider)
            .WithMany()
            .HasForeignKey(c => c.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Translations)
            .WithOne()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.Translations)
            .HasField("_translations")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude();
    }
}
