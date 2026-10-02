using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Notifications.Queries.GetNotifications;

public sealed class GetNotificationsQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    public async Task<IReadOnlyList<NotificationDto>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new AuthenticationFailedException(MessageKeys.Error.Unauthorized);
        var userType = currentUserService.UserType
            ?? throw new AuthenticationFailedException(MessageKeys.Error.Unauthorized);

        var notifications = await unitOfWork.Notifications.ListByRecipientAsync(
            userId,
            userType,
            cancellationToken);

        await unitOfWork.Notifications.MarkAllReadAsync(userId, userType, cancellationToken);

        return notifications
            .Select(n => n.ToDto() with { IsRead = true })
            .ToList();
    }
}
