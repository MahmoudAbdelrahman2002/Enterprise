using Enterprise.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enterprise.Infrastructure.Persistence.Configurations;

public sealed class RoleTranslationConfiguration : IEntityTypeConfiguration<RoleTranslation>
{
    public void Configure(EntityTypeBuilder<RoleTranslation> builder)
    {
        builder.ToTable("RoleTranslations");
        builder.HasKey(t => new { t.RoleId, t.LanguageCode });

        builder.Property(t => t.LanguageCode).IsRequired().HasMaxLength(5);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Description).HasMaxLength(2000);

        builder.HasIndex(t => new { t.LanguageCode, t.Name });

        builder.HasOne<ApplicationRole>()
            .WithMany()
            .HasForeignKey(t => t.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
