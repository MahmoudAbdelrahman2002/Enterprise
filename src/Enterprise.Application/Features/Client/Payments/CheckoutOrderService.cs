using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Notifications;
using Enterprise.Domain.Common;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Enums;
using Enterprise.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Stripe.Checkout;
using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Localization;

namespace Enterprise.Application.Features.Client.Payments;

public interface ICheckoutOrderService
{
    /// <summary>
    /// Creates an order from the paid session's immutable line items and consumes paid basket quantities.
    /// Returns null for unpaid sessions or sessions without valid cart metadata.
    /// Idempotent when an order already exists for the session.
    /// </summary>
    Task<Order?> CompletePaidCheckoutAsync(Session session, CancellationToken cancellationToken = default);
}

public sealed class CheckoutOrderService(
    IUnitOfWork unitOfWork,
    INotificationService notificationService,
    IUserAccountService userAccountService,
    ICheckoutGateway checkoutGateway,
    ILogger<CheckoutOrderService> logger) : ICheckoutOrderService
{
    public async Task<Order?> CompletePaidCheckoutAsync(
        Session session,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation(
                "Ignoring checkout session {SessionId} with payment_status={PaymentStatus}",
                session.Id,
                session.PaymentStatus);
            return null;
        }

        if (session.Metadata is null
            || !session.Metadata.TryGetValue("cartId", out var cartIdRaw)
            || !Guid.TryParse(cartIdRaw, out var cartId))
        {
            logger.LogWarning("Checkout session {SessionId} is missing a valid cartId metadata value", session.Id);
            return null;
        }

        var existing = await unitOfWork.Orders.GetByCheckoutSessionIdAsync(session.Id, cancellationToken);
        if (existing is not null)
        {
            logger.LogInformation("Order already exists for checkout session {SessionId}", session.Id);
            return existing;
        }

        if (!session.Metadata.TryGetValue("userId", out var userIdRaw) || !Guid.TryParse(userIdRaw, out var userId)
            || !session.Metadata.TryGetValue("providerId", out var providerIdRaw) || !Guid.TryParse(providerIdRaw, out var providerId))
        {
            throw new ConflictException(MessageKeys.Payment.UnableToCreate);
        }

        var shoppingCart = await unitOfWork.Carts.GetByIdWithItemsAsync(cartId, cancellationToken);
        if (shoppingCart is not null && (shoppingCart.UserId != userId || shoppingCart.ProviderId != providerId))
            throw new ForbiddenAccessException();

        // Use Stripe's paid line items, not a basket or catalogue that may have changed during payment.
        var paidItems = await checkoutGateway.GetLineItemsAsync(session.Id, cancellationToken);
        var orderItems = new List<OrderItem>();
        foreach (var line in paidItems)
        {
            if (line.Price?.Product?.Metadata is null || !line.Price.Product.Metadata.TryGetValue("productId", out var productIdRaw)
                || !Guid.TryParse(productIdRaw, out var productId) || line.Quantity is null or <= 0
                || line.Quantity > int.MaxValue || line.Price.UnitAmount is null or <= 0)
                throw new ConflictException(MessageKeys.Payment.UnableToCreate);
            orderItems.Add(new OrderItem
            {
                ProductId = productId,
                ProductName = line.Description ?? line.Price.Product.Name,
                UnitPrice = line.Price.UnitAmount.Value / 100m,
                Quantity = (int)line.Quantity.Value
            });
        }
        if (orderItems.Count == 0 || !session.AmountTotal.HasValue
            || orderItems.Sum(item => item.UnitPrice * item.Quantity) != session.AmountTotal.Value / 100m)
            throw new ConflictException(MessageKeys.Payment.UnableToCreate);

        var order = new Order
        {
            StripeCheckoutSessionId = session.Id,
            UserId = userId,
            ProviderId = providerId,
            OrderDateUtc = DateTime.UtcNow,
            TotalAmount = (session.AmountTotal ?? 0m) / 100m,
            Status = OrderStatus.New,
            OrderItems = orderItems
        };

        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                unitOfWork.Orders.Add(order);
                await unitOfWork.SaveChangesAsync(ct);
                await unitOfWork.Carts.ConsumePaidItemsAsync(cartId,
                    orderItems.GroupBy(item => item.ProductId).ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity)), ct);
            }, cancellationToken);
        }
        catch (Exception ex) when (IsDuplicateCheckoutSession(ex))
        {
            logger.LogInformation(ex, "Duplicate checkout session {SessionId}; order already created", session.Id);
            return await unitOfWork.Orders.GetByCheckoutSessionIdAsync(session.Id, cancellationToken);
        }

        logger.LogInformation(
            "Created order {OrderId} from cart {CartId} for checkout session {SessionId}",
            order.Id,
            cartId,
            session.Id);

        await NotifyProviderOfNewOrderAsync(order, cancellationToken);
        return order;
    }

    private async Task NotifyProviderOfNewOrderAsync(Order order, CancellationToken cancellationToken)
    {
        try
        {
            var recipients = await userAccountService.GetActiveProviderRecipientIdsAsync(
                order.ProviderId,
                cancellationToken);
            if (recipients.Count == 0)
            {
                logger.LogWarning("No active provider recipients found for provider {ProviderId}", order.ProviderId);
                return;
            }

            await notificationService.NotifyManyAsync(
                recipients,
                UserType.Provider,
                "New Order",
                "A new order has been created",
                NotificationTypes.NewOrder,
                order.Id,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Order {OrderId} was created but provider notifications failed",
                order.Id);
        }
    }

    private static bool IsDuplicateCheckoutSession(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("unique", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
