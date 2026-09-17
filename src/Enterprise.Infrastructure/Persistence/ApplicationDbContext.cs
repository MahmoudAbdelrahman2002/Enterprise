using System.Reflection;
using Enterprise.Domain.Common;
using Enterprise.Domain.Entities;
using Enterprise.Infrastructure.Identity;
using Enterprise.Infrastructure.Persistence.Extensions;
using Enterprise.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Enterprise.Infrastructure.Persistence;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    AuditableEntitySaveChangesInterceptor auditableEntitySaveChangesInterceptor)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<MarketplaceService> MarketplaceServices => Set<MarketplaceService>();
    public DbSet<MarketplaceServiceTranslation> MarketplaceServiceTranslations => Set<MarketplaceServiceTranslation>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryTranslation> CategoryTranslations => Set<CategoryTranslation>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(auditableEntitySaveChangesInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        modelBuilder.Entity<OtpChallenge>(builder =>
        {
            builder.ToTable("OtpChallenges");
            builder.HasKey(o => o.Id);
            builder.Property(o => o.Email).IsRequired().HasMaxLength(256);
            builder.Property(o => o.CodeHash).IsRequired().HasMaxLength(128);
            builder.HasIndex(o => new { o.Email, o.Purpose });
        });

        modelBuilder.Entity<RefreshToken>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ApiKey>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(k => k.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Provider>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ApplicationRole>(builder =>
        {
            builder.Property(r => r.RoleType).IsRequired();
            builder.Property(r => r.IsSystem).IsRequired();
            builder.Property(r => r.ProviderId).IsRequired(false);

            builder.HasIndex(r => new { r.RoleType, r.ProviderId });

            builder.HasOne<Provider>()
                .WithMany()
                .HasForeignKey(r => r.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApplicationUser>(builder =>
        {
            builder.Property(u => u.IsSystem).IsRequired();
            builder.Property(u => u.ProviderId).IsRequired(false);

            builder.HasIndex(u => u.ProviderId);

            builder.HasOne<Provider>()
                .WithMany()
                .HasForeignKey(u => u.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).AddSoftDeleteQueryFilter();
            }
        }
    }
}
