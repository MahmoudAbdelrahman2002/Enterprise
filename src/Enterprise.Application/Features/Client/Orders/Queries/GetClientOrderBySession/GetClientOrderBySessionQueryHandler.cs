using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Orders;
using Enterprise.Application.Features.Orders.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Client.Orders.Queries.GetClientOrderBySession;

public sealed class GetClientOrderBySessionQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetClientOrderBySessionQuery, OrderDetailDto>
{
    public async Task<OrderDetailDto> Handle(
        GetClientOrderBySessionQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new AuthenticationFailedException(MessageKeys.Error.Unauthorized);

        var order = await unitOfWork.Orders.GetByCheckoutSessionIdForUserAsync(
                request.SessionId,
                userId,
                cancellationToken)
            ?? throw NotFoundException.For(nameof(Order), request.SessionId);

        return order.ToDetailDto();
    }
}
