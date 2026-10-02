using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Orders;
using Enterprise.Application.Features.Orders.DTOs;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Client.Orders.Queries.GetClientOrdersByProvider;

public sealed class GetClientOrdersByProviderQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetClientOrdersByProviderQuery, IReadOnlyList<OrderListItemDto>>
{
    public async Task<IReadOnlyList<OrderListItemDto>> Handle(
        GetClientOrdersByProviderQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new AuthenticationFailedException(MessageKeys.Error.Unauthorized);

        var orders = await unitOfWork.Orders.ListByUserAndProviderAsync(
            userId,
            request.ProviderId,
            cancellationToken);

        return orders.Select(o => o.ToListItemDto()).ToList();
    }
}
