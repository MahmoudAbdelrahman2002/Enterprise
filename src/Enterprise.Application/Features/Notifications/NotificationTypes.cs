namespace Enterprise.Application.Features.Notifications;

/// <summary>
/// Catalog of notification_type values stored in the DB and sent in FCM data payloads.
/// Add new constants here when wiring future business events — no migration required.
/// </summary>
public static class NotificationTypes
{
    public const string NewProviderRegistration = "new_provider_registration";
    public const string NewOrder = "new_order";
    public const string OrderStatusChanged = "order_status_changed";
    // Upcoming (not wired yet) — reserved names for the guide / frontend contract:
    // public const string Order = "order";
}
