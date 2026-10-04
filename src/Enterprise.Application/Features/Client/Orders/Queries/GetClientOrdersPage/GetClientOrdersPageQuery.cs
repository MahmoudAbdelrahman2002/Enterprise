using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Orders;
using Enterprise.Application.Features.Orders.DTOs;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Client.Orders.Queries.GetClientOrdersPage;

public sealed record GetClientOrdersPageQuery : PaginationParams, IRequest<PagedResult<OrderListItemDto>>
{
    public Guid? ProviderId { get; init; }
}

public sealed class GetClientOrdersPageQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : IRequestHandler<GetClientOrdersPageQuery, PagedResult<OrderListItemDto>>
{
    public async Task<PagedResult<OrderListItemDto>> Handle(GetClientOrdersPageQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new AuthenticationFailedException(MessageKeys.Error.Unauthorized);
        var (orders, count) = await unitOfWork.Orders.SearchForUserAsync(userId, request.ProviderId, request.PageNumber, request.PageSize, cancellationToken);
        return new PagedResult<OrderListItemDto>(orders.Select(order => order.ToListItemDto()).ToArray(), count, request.PageNumber, request.PageSize);
    }
}
