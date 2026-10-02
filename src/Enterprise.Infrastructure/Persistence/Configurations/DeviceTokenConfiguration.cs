using Enterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enterprise.Infrastructure.Persistence.Configurations;

public sealed class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
{
    public void Configure(EntityTypeBuilder<DeviceToken> builder)
    {
        builder.ToTable("DeviceTokens");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Token).IsRequired().HasMaxLength(512);
        builder.Property(d => d.Platform).HasMaxLength(32);

        builder.HasIndex(d => d.Token).IsUnique();
        builder.HasIndex(d => d.UserId);
    }
}
