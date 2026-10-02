using Enterprise.Domain.Enums;

namespace Enterprise.Application.Common.Interfaces;

public interface INotificationService
{
    Task NotifyAsync(
        Guid recipientUserId,
        UserType recipientUserType,
        string title,
        string body,
        string notificationType,
        Guid? relatedEntityId = null,
        CancellationToken cancellationToken = default);

    Task NotifyManyAsync(
        IReadOnlyList<Guid> recipientUserIds,
        UserType recipientUserType,
        string title,
        string body,
        string notificationType,
        Guid? relatedEntityId = null,
        CancellationToken cancellationToken = default);
}
