using Enterprise.Domain.Entities;
using Enterprise.Domain.Enums;

namespace Enterprise.Domain.Interfaces;

public interface INotificationRepository : IRepository<Notification>
{
    Task<IReadOnlyList<Notification>> ListByRecipientAsync(
        Guid recipientUserId,
        UserType recipientUserType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> ListByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(
        Guid recipientUserId,
        UserType recipientUserType,
        CancellationToken cancellationToken = default);

    Task MarkAllReadAsync(
        Guid recipientUserId,
        UserType recipientUserType,
        CancellationToken cancellationToken = default);
}
