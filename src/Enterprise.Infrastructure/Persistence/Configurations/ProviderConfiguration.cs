using Enterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enterprise.Infrastructure.Persistence.Configurations;

public sealed class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        builder.ToTable("Providers");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.CompanyName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.PhoneNumber).HasMaxLength(40);

        builder.HasIndex(p => p.UserId).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(p => p.CompanyName);
        builder.HasIndex(p => p.ServiceId);

        builder.HasOne(p => p.Service)
            .WithMany()
            .HasForeignKey(p => p.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
