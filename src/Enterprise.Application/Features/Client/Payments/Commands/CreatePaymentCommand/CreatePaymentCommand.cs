using Enterprise.Application.Common.Validation;
using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Settings;
using Enterprise.Domain.Enums;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Options;
using Stripe.Checkout;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Enterprise.Application.Features.Client.Payments.Commands.CreatePaymentCommand;

public class CreatePaymentCommand(Guid providerId) : IRequest<CreatePaymentSessionDto>
{
    public Guid ProviderId { get; } = providerId;
}

public sealed class CreatePaymentCommandHandler(
    IUnitOfWork unitOfWork,
    IClientProviderQueryService providers,
    ICurrentUserService currentUserService,
    ICurrentCulture currentCulture,
    IOptions<StripeSettings> stripeOptions,
    ICheckoutGateway checkoutGateway) : IRequestHandler<CreatePaymentCommand, CreatePaymentSessionDto>
{
    public async Task<CreatePaymentSessionDto> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        var stripe = stripeOptions.Value;
        var culture = currentCulture.LanguageCode;
        var userId = currentUserService.UserId
            ?? throw new AuthenticationFailedException(MessageKeys.Error.Unauthorized);

        if (string.IsNullOrWhiteSpace(stripe.SecretKey)
            || !ValidRedirect(stripe.SuccessUrl) || !ValidRedirect(stripe.CancelUrl))
        {
            throw new PaymentUnavailableException();
        }

        if (await providers.GetByIdAsync(request.ProviderId, cancellationToken) is null)
            throw NotFoundException.For("Provider", request.ProviderId);

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
                || item.Quantity < 1 || item.Quantity > ValidationPolicy.QuantityMax
                || product.Price < .01m || product.Price > ValidationPolicy.PriceMax || decimal.Round(product.Price, 2) != product.Price
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

        var lineItems = cart.Items.OrderBy(item => item.ProductId).Select(item =>
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
                        Name = string.IsNullOrWhiteSpace(name) ? product.Sku : name,
                        Metadata = new Dictionary<string, string> { ["productId"] = product.Id.ToString() }
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

        // Identical cart contents and return URLs reuse the same Checkout session on retries.
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(options))));
        var session = await checkoutGateway.CreateAsync(options, "checkout-" + fingerprint, cancellationToken);
        if (string.IsNullOrWhiteSpace(session.Id) || !Uri.TryCreate(session.Url, UriKind.Absolute, out var checkoutUrl)
            || checkoutUrl.Scheme != Uri.UriSchemeHttps) throw new PaymentUnavailableException();

        return new CreatePaymentSessionDto
        {
            SessionId = session.Id,
            Url = session.Url ?? string.Empty,
            Metadata = session.Metadata ?? new Dictionary<string, string>()
        };
    }

    private static bool ValidRedirect(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
