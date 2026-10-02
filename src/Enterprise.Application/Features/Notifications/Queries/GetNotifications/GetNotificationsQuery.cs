using MediatR;

namespace Enterprise.Application.Features.Notifications.Queries.GetNotifications;

public sealed record GetNotificationsQuery : IRequest<IReadOnlyList<NotificationDto>>;
