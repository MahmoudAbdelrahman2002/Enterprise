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

namespace Enterprise.Application.Features.Provider.Orders.Commands.UpdateProviderOrderStatus;

public sealed class UpdateProviderOrderStatusCommandHandler(
    IUnitOfWork unitOfWork,
    IProviderContext providerContext,
    INotificationService notificationService,
    IUserAccountService userAccountService,
    ILogger<UpdateProviderOrderStatusCommandHandler> logger)
    : IRequestHandler<UpdateProviderOrderStatusCommand, OrderDetailDto>
{
    public async Task<OrderDetailDto> Handle(
        UpdateProviderOrderStatusCommand request,
        CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);
        var order = await unitOfWork.Orders.GetTrackedByIdForProviderAsync(
                request.OrderId,
                providerId,
                cancellationToken)
            ?? throw NotFoundException.For(nameof(Order), request.OrderId);

        if (order.IsHistorical || !OrderStatusTransitions.CanTransition(order.Status, request.Status))
        {
            throw new ConflictException(MessageKeys.Order.InvalidStatusTransition);
        }

        order.ChangeStatus(request.Status);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await NotifyUserOfOrderStatusChangeAsync(order, cancellationToken);

        logger.LogInformation(
            "Updated order {OrderId} status to {Status} for provider {ProviderId}",
            order.Id,
            order.Status,
            providerId);

        return order.ToDetailDto();
    }
    private async Task NotifyUserOfOrderStatusChangeAsync(Order order, CancellationToken cancellationToken)
    {
        try
        {
            var user = await userAccountService.FindByIdAsync(order.UserId, cancellationToken);
            if (user is null)
            {
                logger.LogWarning("User not found for order {OrderId}", order.Id);
                return;
            }

            await notificationService.NotifyAsync(
                user.Id,
                UserType.Client,
                "Order status updated",
                $"Your order is now {order.Status}.",
                NotificationTypes.OrderStatusChanged,
                order.Id,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Order {OrderId} status was updated but the client notification failed",
                order.Id);
        }
    }
}
