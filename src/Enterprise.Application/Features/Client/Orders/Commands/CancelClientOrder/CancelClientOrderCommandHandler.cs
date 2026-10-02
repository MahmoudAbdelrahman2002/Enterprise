using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Notifications;
using Enterprise.Application.Features.Orders;
using Enterprise.Application.Features.Orders.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Enums;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Client.Orders.Commands.CancelClientOrder;

public sealed class CancelClientOrderCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    INotificationService notificationService,
    IUserAccountService userAccountService,
    ILogger<CancelClientOrderCommandHandler> logger)
    : IRequestHandler<CancelClientOrderCommand, OrderDetailDto>
{
    public async Task<OrderDetailDto> Handle(
        CancelClientOrderCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new AuthenticationFailedException(MessageKeys.Error.Unauthorized);

        var order = await unitOfWork.Orders.GetTrackedByIdForUserAsync(
                request.OrderId,
                userId,
                cancellationToken)
            ?? throw NotFoundException.For(nameof(Order), request.OrderId);

        if (order.Status != OrderStatus.Pending)
        {
            throw new ConflictException(MessageKeys.Order.InvalidStatusTransition);
        }

        order.ChangeStatus(OrderStatus.Cancelled);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await NotifyProviderAsync(order, cancellationToken);

        return order.ToDetailDto();
    }

    private async Task NotifyProviderAsync(Order order, CancellationToken cancellationToken)
    {
        try
        {
            var recipients = await userAccountService.GetActiveProviderRecipientIdsAsync(
                order.ProviderId,
                cancellationToken);
            if (recipients.Count == 0)
            {
                return;
            }

            await notificationService.NotifyManyAsync(
                recipients,
                UserType.Provider,
                "Order cancelled",
                "A client cancelled an order.",
                NotificationTypes.OrderStatusChanged,
                order.Id,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Order {OrderId} was cancelled but the provider notification failed", order.Id);
        }
    }
}
