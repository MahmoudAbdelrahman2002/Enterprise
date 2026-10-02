using Enterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enterprise.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title).IsRequired().HasMaxLength(200);
        builder.Property(n => n.Body).IsRequired().HasMaxLength(2000);
        builder.Property(n => n.NotificationType).IsRequired().HasMaxLength(100);
        builder.Property(n => n.RecipientUserType).IsRequired();
        builder.Property(n => n.IsRead).IsRequired();

        builder.HasIndex(n => new { n.RecipientUserId, n.IsRead });
        builder.HasIndex(n => new { n.RecipientUserId, n.CreatedAtUtc });
    }
}
