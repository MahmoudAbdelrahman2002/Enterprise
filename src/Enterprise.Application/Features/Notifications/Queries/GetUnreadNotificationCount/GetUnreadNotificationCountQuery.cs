using MediatR;

namespace Enterprise.Application.Features.Notifications.Queries.GetUnreadNotificationCount;

public sealed record GetUnreadNotificationCountQuery : IRequest<UnreadNotificationCountDto>;
