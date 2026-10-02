using Enterprise.Domain.Common;
using Enterprise.Domain.Enums;

namespace Enterprise.Domain.Entities;

public class Notification : BaseAuditableEntity
{
    public Guid RecipientUserId { get; set; }
    public UserType RecipientUserType { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string NotificationType { get; set; } = "";
    public Guid? RelatedEntityId { get; set; }
    public bool IsRead { get; set; }

    public Notification()
    {
    }

    public Notification(
        Guid recipientUserId,
        UserType recipientUserType,
        string title,
        string body,
        string notificationType,
        Guid? relatedEntityId = null)
    {
        RecipientUserId = recipientUserId;
        RecipientUserType = recipientUserType;
        Title = title;
        Body = body;
        NotificationType = notificationType;
        RelatedEntityId = relatedEntityId;
        IsRead = false;
    }

    public void MarkAsRead() => IsRead = true;
}
