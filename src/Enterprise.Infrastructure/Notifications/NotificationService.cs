using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Enums;
using Enterprise.Domain.Interfaces;
using Enterprise.Infrastructure.BackgroundJobs.Jobs;
using Microsoft.Extensions.Logging;

namespace Enterprise.Infrastructure.Notifications;

public sealed class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobService _backgroundJobs;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IUnitOfWork unitOfWork,
        IBackgroundJobService backgroundJobs,
        ILogger<NotificationService> logger)
    {
        _unitOfWork = unitOfWork;
        _backgroundJobs = backgroundJobs;
        _logger = logger;
    }

    public Task NotifyAsync(
        Guid recipientUserId,
        UserType recipientUserType,
        string title,
        string body,
        string notificationType,
        Guid? relatedEntityId = null,
        CancellationToken cancellationToken = default) =>
        NotifyManyAsync(
            [recipientUserId],
            recipientUserType,
            title,
            body,
            notificationType,
            relatedEntityId,
            cancellationToken);

    public async Task NotifyManyAsync(
        IReadOnlyList<Guid> recipientUserIds,
        UserType recipientUserType,
        string title,
        string body,
        string notificationType,
        Guid? relatedEntityId = null,
        CancellationToken cancellationToken = default)
    {
        var distinctIds = recipientUserIds.Distinct().ToList();
        if (distinctIds.Count == 0)
        {
            return;
        }

        var notifications = distinctIds
            .Select(userId => new Domain.Entities.Notification(
                userId,
                recipientUserType,
                title,
                body,
                notificationType,
                relatedEntityId))
            .ToList();

        foreach (var notification in notifications)
        {
            _unitOfWork.Notifications.Add(notification);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var ids = notifications.Select(n => n.Id).ToArray();
        try
        {
            _backgroundJobs.Enqueue<SendNotificationPushJob>(job =>
                job.ExecuteAsync(ids, CancellationToken.None));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Saved {Count} notification(s) but failed to enqueue the push job",
                ids.Length);
        }
    }
}
