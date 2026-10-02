using Enterprise.Infrastructure.Notifications;

namespace Enterprise.Infrastructure.BackgroundJobs.Jobs;

public sealed class SendNotificationPushJob(FcmPushSender pushSender)
{
    public Task ExecuteAsync(Guid[] notificationIds, CancellationToken cancellationToken) =>
        pushSender.SendSavedPushesAsync(notificationIds, cancellationToken);
}
