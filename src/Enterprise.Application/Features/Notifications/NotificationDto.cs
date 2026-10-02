namespace Enterprise.Application.Features.Notifications;

public sealed record NotificationDto(
    Guid Id,
    string Title,
    string Body,
    string NotificationType,
    Guid? NotificationId,
    bool IsRead,
    DateTime CreatedAtUtc);

public sealed record UnreadNotificationCountDto(int UnreadCount);

public static class NotificationMapping
{
    public static NotificationDto ToDto(this Domain.Entities.Notification notification) =>
        new(
            notification.Id,
            notification.Title,
            notification.Body,
            notification.NotificationType,
            notification.RelatedEntityId,
            notification.IsRead,
            notification.CreatedAtUtc);
}
