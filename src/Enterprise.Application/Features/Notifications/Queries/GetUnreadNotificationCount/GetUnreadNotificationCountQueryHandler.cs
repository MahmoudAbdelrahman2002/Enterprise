using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Notifications.Queries.GetUnreadNotificationCount;

public sealed class GetUnreadNotificationCountQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetUnreadNotificationCountQuery, UnreadNotificationCountDto>
{
    public async Task<UnreadNotificationCountDto> Handle(
        GetUnreadNotificationCountQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new AuthenticationFailedException(MessageKeys.Error.Unauthorized);
        var userType = currentUserService.UserType
            ?? throw new AuthenticationFailedException(MessageKeys.Error.Unauthorized);

        var count = await unitOfWork.Notifications.CountUnreadAsync(userId, userType, cancellationToken);
        return new UnreadNotificationCountDto(count);
    }
}
