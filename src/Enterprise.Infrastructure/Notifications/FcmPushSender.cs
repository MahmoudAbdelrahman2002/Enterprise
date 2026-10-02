using Enterprise.Application.Common.Settings;
using Enterprise.Domain.Interfaces;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterprise.Infrastructure.Notifications;

public sealed class FcmPushSender
{
    private const int PushBatchSize = 500;

    private readonly IUnitOfWork _unitOfWork;
    private readonly FirebaseSettings _settings;
    private readonly ILogger<FcmPushSender> _logger;
    private readonly bool _firebaseReady;

    public FcmPushSender(
        IUnitOfWork unitOfWork,
        IOptions<FirebaseSettings> settings,
        ILogger<FcmPushSender> logger)
    {
        _unitOfWork = unitOfWork;
        _settings = settings.Value;
        _logger = logger;
        _firebaseReady = EnsureFirebaseApp();
    }

    public async Task SendSavedPushesAsync(
        IReadOnlyList<Guid> notificationIds,
        CancellationToken cancellationToken = default)
    {
        if (notificationIds.Count == 0)
        {
            return;
        }

        if (!_firebaseReady)
        {
            _logger.LogWarning(
                "Firebase is not configured; {Count} notification(s) saved to DB without FCM push",
                notificationIds.Count);
            return;
        }

        var notifications = await _unitOfWork.Notifications.ListByIdsAsync(notificationIds, cancellationToken);
        if (notifications.Count == 0)
        {
            return;
        }

        var userIds = notifications.Select(n => n.RecipientUserId).Distinct().ToList();
        var tokens = await _unitOfWork.DeviceTokens.ListByUserIdsAsync(userIds, cancellationToken);
        if (tokens.Count == 0)
        {
            return;
        }

        var tokensByUser = tokens
            .GroupBy(t => t.UserId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var pending = new List<(string Token, Message Message)>();
        foreach (var notification in notifications)
        {
            if (!tokensByUser.TryGetValue(notification.RecipientUserId, out var userTokens))
            {
                continue;
            }

            foreach (var device in userTokens)
            {
                pending.Add((device.Token, new Message
                {
                    Token = device.Token,
                    Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = notification.Title,
                        Body = notification.Body
                    },
                    Data = new Dictionary<string, string>
                    {
                        ["notification_id"] = notification.Id.ToString(),
                        ["notification_type"] = notification.NotificationType,
                        ["related_id"] = notification.RelatedEntityId?.ToString() ?? "",
                        ["user_type"] = notification.RecipientUserType.ToString()
                    }
                }));
            }
        }

        foreach (var chunk in pending.Chunk(PushBatchSize))
        {
            var response = await FirebaseMessaging.DefaultInstance.SendEachAsync(
                chunk.Select(item => item.Message),
                cancellationToken);

            for (var i = 0; i < response.Responses.Count; i++)
            {
                var send = response.Responses[i];
                if (send.IsSuccess)
                {
                    continue;
                }

                if (send.Exception is FirebaseMessagingException ex &&
                    ex.MessagingErrorCode is MessagingErrorCode.Unregistered
                        or MessagingErrorCode.InvalidArgument)
                {
                    _logger.LogInformation(
                        "Removing invalid FCM token: {Error}",
                        ex.MessagingErrorCode);
                    await _unitOfWork.DeviceTokens.RemoveByTokenAsync(chunk[i].Token, cancellationToken);
                    continue;
                }

                _logger.LogWarning(send.Exception, "FCM push failed for a device token");
            }
        }
    }

    private bool EnsureFirebaseApp()
    {
        if (FirebaseApp.DefaultInstance is not null)
        {
            return true;
        }

        if (!_settings.IsConfigured)
        {
            return false;
        }

        try
        {
            GoogleCredential credential;
            if (!string.IsNullOrWhiteSpace(_settings.CredentialsJson))
            {
                credential = GoogleCredential.FromJson(_settings.CredentialsJson);
            }
            else
            {
                credential = GoogleCredential.FromFile(_settings.CredentialsPath);
            }

            FirebaseApp.Create(new AppOptions { Credential = credential });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize FirebaseApp; FCM push will be skipped");
            return false;
        }
    }
}
