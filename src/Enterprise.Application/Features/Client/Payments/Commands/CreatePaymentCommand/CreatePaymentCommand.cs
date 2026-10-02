using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Settings;
using Enterprise.Domain.Enums;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Options;
using Stripe.Checkout;

namespace Enterprise.Application.Features.Client.Payments.Commands.CreatePaymentCommand;

public class CreatePaymentCommand(Guid providerId) : IRequest<CreatePaymentSessionDto>
{
    public Guid ProviderId { get; } = providerId;
}

public sealed class CreatePaymentCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ICurrentCulture currentCulture,
    IOptions<StripeSettings> stripeOptions) : IRequestHandler<CreatePaymentCommand, CreatePaymentSessionDto>
{
    public async Task<CreatePaymentSessionDto> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        var stripe = stripeOptions.Value;
        var culture = currentCulture.LanguageCode;
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedAccessException();

        var cart = await unitOfWork.Carts.GetCartByProviderIdAndUserIdAsync(
            request.ProviderId, userId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Cart.NotFound);

        if (cart.Items.Count == 0)
        {
            throw new NotFoundException(MessageKeys.Cart.NotFound);
        }

        foreach (var item in cart.Items)
        {
            var product = item.Product;
            if (product is null
                || product.Status != ProductStatus.Active
                || product.Category is null
                || !product.Category.IsActive
                || product.Category.ProviderId != request.ProviderId)
            {
                throw new ConflictException(MessageKeys.Payment.UnableToCreate);
            }
        }

        var currency = string.IsNullOrWhiteSpace(stripe.Currency) ? "usd" : stripe.Currency;

        var metadata = new Dictionary<string, string>
        {
            ["cartId"] = cart.Id.ToString(),
            ["userId"] = userId.ToString(),
            ["providerId"] = request.ProviderId.ToString(),
            ["locale"] = culture
        };

        var lineItems = cart.Items.Select(item =>
        {
            var product = item.Product;
            var (name, _) = product.ResolveContent(culture);

            return new SessionLineItemOptions
            {
                Quantity = item.Quantity,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = currency,
                    UnitAmount = (long)(product.Price * 100),
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = string.IsNullOrWhiteSpace(name) ? product.Sku : name
                    }
                }
            };
        }).ToList();

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = stripe.SuccessUrl,
            CancelUrl = stripe.CancelUrl,
            LineItems = lineItems,
            ClientReferenceId = cart.Id.ToString(),
            Metadata = metadata,
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                Metadata = metadata
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options, cancellationToken: cancellationToken);

        return new CreatePaymentSessionDto
        {
            SessionId = session.Id,
            Url = session.Url ?? string.Empty,
            Metadata = session.Metadata ?? new Dictionary<string, string>()
        };
    }
}
