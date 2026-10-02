using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Notifications;
using Enterprise.Domain.Common;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Enums;
using Enterprise.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Stripe.Checkout;

namespace Enterprise.Application.Features.Client.Payments;

public interface ICheckoutOrderService
{
    /// <summary>
    /// Creates an order from a paid Stripe Checkout session and clears the cart.
    /// Returns null when the session cannot be fulfilled (missing cart, empty cart, etc.).
    /// Idempotent when an order already exists for the session.
    /// </summary>
    Task<Order?> CompletePaidCheckoutAsync(Session session, CancellationToken cancellationToken = default);
}

public sealed class CheckoutOrderService(
    IUnitOfWork unitOfWork,
    INotificationService notificationService,
    IUserAccountService userAccountService,
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

        var shoppingCart = await unitOfWork.Carts.GetByIdWithItemsAsync(cartId, cancellationToken);
        if (shoppingCart is null)
        {
            logger.LogWarning("Shopping cart not found for cart ID {CartId}", cartId);
            return null;
        }

        if (shoppingCart.Items.Count == 0)
        {
            logger.LogWarning("Shopping cart {CartId} has no items; skipping order creation", cartId);
            return null;
        }

        var language = session.Metadata.TryGetValue("locale", out var locale)
            ? locale
            : SupportedLanguages.English;

        var order = new Order
        {
            StripeCheckoutSessionId = session.Id,
            UserId = shoppingCart.UserId,
            ProviderId = shoppingCart.ProviderId,
            OrderDateUtc = DateTime.UtcNow,
            TotalAmount = (session.AmountTotal ?? 0m) / 100m,
            Status = OrderStatus.Pending,
            OrderItems = shoppingCart.Items.Select(i =>
            {
                var (name, _) = i.Product.ResolveContent(language);
                return new OrderItem
                {
                    ProductId = i.ProductId,
                    ProductName = string.IsNullOrWhiteSpace(name) ? i.Product.Sku : name,
                    UnitPrice = i.Product.Price,
                    Quantity = i.Quantity
                };
            }).ToList()
        };

        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                unitOfWork.Orders.Add(order);
                await unitOfWork.SaveChangesAsync(ct);
                await unitOfWork.Carts.DeleteByIdAsync(shoppingCart.Id, ct);
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
