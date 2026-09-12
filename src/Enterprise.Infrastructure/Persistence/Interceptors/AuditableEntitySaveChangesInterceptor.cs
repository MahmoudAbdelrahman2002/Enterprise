using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Enterprise.Infrastructure.Persistence.Interceptors;

/// <summary>
/// The single place that enforces two cross-cutting rules for every entity in the model, so
/// individual handlers/repositories never have to remember to apply them:
///   1. Stamp CreatedAt/By and LastModifiedAt/By on every <see cref="BaseAuditableEntity"/>.
///   2. Rewrite a physical delete of an <see cref="ISoftDelete"/> entity into an update that
///      just flips <c>IsDeleted</c> - callers still just say <c>repository.Remove(entity)</c>,
///      they never need to know soft-delete is happening underneath.
/// </summary>
public sealed class AuditableEntitySaveChangesInterceptor(ICurrentUserService currentUserService, IDateTime dateTime)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context is null) return;

        var currentUser = currentUserService.Email ?? "system";

        foreach (var entry in context.ChangeTracker.Entries<BaseAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = dateTime.UtcNow;
                    entry.Entity.CreatedBy = currentUser;
                    break;
                case EntityState.Modified:
                    entry.Entity.LastModifiedAtUtc = dateTime.UtcNow;
                    entry.Entity.LastModifiedBy = currentUser;
                    break;
            }
        }

        foreach (var entry in context.ChangeTracker.Entries<ISoftDelete>())
        {
            if (entry.State != EntityState.Deleted) continue;

            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAtUtc = dateTime.UtcNow;
            entry.Entity.DeletedBy = currentUser;
        }
    }
}
