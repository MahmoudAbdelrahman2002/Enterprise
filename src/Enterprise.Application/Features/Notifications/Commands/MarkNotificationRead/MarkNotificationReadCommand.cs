using MediatR;

namespace Enterprise.Application.Features.Notifications.Commands.MarkNotificationRead;

public sealed record MarkNotificationReadCommand(Guid NotificationId) : IRequest;
