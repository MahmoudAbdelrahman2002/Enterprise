using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Orders;
using Enterprise.Application.Features.Orders.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Client.Orders.Queries.GetClientOrderById;

public sealed class GetClientOrderByIdQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService) : IRequestHandler<GetClientOrderByIdQuery, OrderDetailDto>
{
    public async Task<OrderDetailDto> Handle(GetClientOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new AuthenticationFailedException(MessageKeys.Error.Unauthorized);

        var order = await unitOfWork.Orders.GetByIdForUserAsync(request.OrderId, userId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Order), request.OrderId);

        return order.ToDetailDto();
    }
}
