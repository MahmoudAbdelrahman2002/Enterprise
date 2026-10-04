using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Orders;
using Enterprise.Application.Features.Orders.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace Enterprise.Application.Features.Client.Payments.Commands.ConfirmClientCheckout;

public sealed record ConfirmClientCheckoutCommand(string SessionId) : IRequest<OrderDetailDto>;

public sealed class ConfirmClientCheckoutCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ICheckoutOrderService checkoutOrderService,
    ICheckoutGateway checkoutGateway,
    ILogger<ConfirmClientCheckoutCommandHandler> logger)
    : IRequestHandler<ConfirmClientCheckoutCommand, OrderDetailDto>
{
    public async Task<OrderDetailDto> Handle(
        ConfirmClientCheckoutCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new AuthenticationFailedException(MessageKeys.Error.Unauthorized);

        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            throw NotFoundException.For(nameof(Order), request.SessionId ?? string.Empty);
        }

        var existing = await unitOfWork.Orders.GetByCheckoutSessionIdForUserAsync(
            request.SessionId,
            userId,
            cancellationToken);
        if (existing is not null)
        {
            return existing.ToDetailDto();
        }

        Session session;
        try
        {
            session = await checkoutGateway.GetAsync(request.SessionId, cancellationToken);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Unable to load Stripe checkout session {SessionId}", request.SessionId);
            throw NotFoundException.For(nameof(Order), request.SessionId);
        }

        if (session.Metadata is null
            || !session.Metadata.TryGetValue("userId", out var userIdRaw)
            || !Guid.TryParse(userIdRaw, out var sessionUserId)
            || sessionUserId != userId)
        {
            throw new ForbiddenAccessException();
        }

        if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException(MessageKeys.Payment.UnableToCreate);
        }

        var order = await checkoutOrderService.CompletePaidCheckoutAsync(session, cancellationToken)
            ?? throw NotFoundException.For(nameof(Order), request.SessionId);

        var detail = await unitOfWork.Orders.GetByCheckoutSessionIdForUserAsync(
                order.StripeCheckoutSessionId!,
                userId,
                cancellationToken)
            ?? order;

        return detail.ToDetailDto();
    }
}
