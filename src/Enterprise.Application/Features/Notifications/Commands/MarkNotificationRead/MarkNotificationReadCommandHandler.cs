using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Notifications.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<MarkNotificationReadCommand>
{
    public async Task Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new AuthenticationFailedException(MessageKeys.Error.Unauthorized);
        var userType = currentUserService.UserType
            ?? throw new AuthenticationFailedException(MessageKeys.Error.Unauthorized);

        var notification = await unitOfWork.Notifications.GetByIdAsync(request.NotificationId, cancellationToken);
        if (notification is null
            || notification.RecipientUserId != userId
            || notification.RecipientUserType != userType)
        {
            throw NotFoundException.For(nameof(Notification), request.NotificationId);
        }

        if (!notification.IsRead)
        {
            notification.MarkAsRead();
            unitOfWork.Notifications.Update(notification);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
