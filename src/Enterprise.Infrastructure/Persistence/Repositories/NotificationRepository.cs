using Enterprise.Domain.Entities;
using Enterprise.Domain.Enums;
using Enterprise.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Enterprise.Infrastructure.Persistence.Repositories;

public class NotificationRepository(ApplicationDbContext context)
    : GenericRepository<Notification>(context), INotificationRepository
{
    public Task<int> CountByRecipientAsync(Guid recipientUserId, UserType recipientUserType, CancellationToken cancellationToken = default) =>
        DbSet.CountAsync(n => n.RecipientUserId == recipientUserId && n.RecipientUserType == recipientUserType, cancellationToken);

    public async Task<IReadOnlyList<Notification>> ListPageByRecipientAsync(Guid recipientUserId, UserType recipientUserType, int pageNumber, int pageSize, CancellationToken cancellationToken = default) =>
        await DbSet.AsNoTracking().Where(n => n.RecipientUserId == recipientUserId && n.RecipientUserType == recipientUserType)
            .OrderByDescending(n => n.CreatedAtUtc).ThenByDescending(n => n.Id)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

    public async Task MarkPageReadAsync(Guid recipientUserId, UserType recipientUserType, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        await DbSet.Where(n => n.RecipientUserId == recipientUserId && n.RecipientUserType == recipientUserType && ids.Contains(n.Id) && !n.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true), cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> ListByRecipientAsync(
        Guid recipientUserId,
        UserType recipientUserType,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(n => n.RecipientUserId == recipientUserId && n.RecipientUserType == recipientUserType)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> ListByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(n => ids.Contains(n.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountUnreadAsync(
        Guid recipientUserId,
        UserType recipientUserType,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.CountAsync(
            n => n.RecipientUserId == recipientUserId
                && n.RecipientUserType == recipientUserType
                && !n.IsRead,
            cancellationToken);
    }

    public async Task MarkAllReadAsync(
        Guid recipientUserId,
        UserType recipientUserType,
        CancellationToken cancellationToken = default)
    {
        await DbSet
            .Where(n =>
                n.RecipientUserId == recipientUserId
                && n.RecipientUserType == recipientUserType
                && !n.IsRead)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(n => n.IsRead, true),
                cancellationToken);
    }
}
