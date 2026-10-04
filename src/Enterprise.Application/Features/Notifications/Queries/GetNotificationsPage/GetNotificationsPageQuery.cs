using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Notifications.Queries.GetNotificationsPage;

public sealed record GetNotificationsPageQuery : PaginationParams, IRequest<PagedResult<NotificationDto>>;

public sealed class GetNotificationsPageQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : IRequestHandler<GetNotificationsPageQuery, PagedResult<NotificationDto>>
{
    public async Task<PagedResult<NotificationDto>> Handle(GetNotificationsPageQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new AuthenticationFailedException(MessageKeys.Error.Unauthorized);
        var userType = currentUser.UserType ?? throw new AuthenticationFailedException(MessageKeys.Error.Unauthorized);
        var count = await unitOfWork.Notifications.CountByRecipientAsync(userId, userType, cancellationToken);
        var notifications = await unitOfWork.Notifications.ListPageByRecipientAsync(userId, userType, request.PageNumber, request.PageSize, cancellationToken);
        // Opening an inbox page reads only that page, not notifications the user has never seen.
        await unitOfWork.Notifications.MarkPageReadAsync(userId, userType, notifications.Select(n => n.Id).ToArray(), cancellationToken);
        return new PagedResult<NotificationDto>(notifications.Select(n => n.ToDto() with { IsRead = true }).ToArray(), count, request.PageNumber, request.PageSize);
    }
}
