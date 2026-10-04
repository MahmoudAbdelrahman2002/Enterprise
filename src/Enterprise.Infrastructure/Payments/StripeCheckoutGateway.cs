using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Settings;
using Enterprise.Application.Features.Client.Payments;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Enterprise.Infrastructure.Payments;

public sealed class StripeCheckoutGateway(IOptions<StripeSettings> settings, ILogger<StripeCheckoutGateway> logger) : ICheckoutGateway
{
    private StripeClient Client()
    {
        if (string.IsNullOrWhiteSpace(settings.Value.SecretKey)) throw new PaymentUnavailableException();
        return new StripeClient(settings.Value.SecretKey);
    }

    private SessionService Service() => new(Client());

    public async Task<Session> CreateAsync(SessionCreateOptions options, string idempotencyKey, CancellationToken cancellationToken)
    {
        try
        {
            return await Service().CreateAsync(options, new RequestOptions { IdempotencyKey = idempotencyKey }, cancellationToken);
        }
        catch (StripeException exception)
        {
            logger.LogWarning("Stripe checkout request failed with code {Code}", exception.StripeError?.Code);
            throw new PaymentUnavailableException();
        }
    }

    public Task<Session> GetAsync(string sessionId, CancellationToken cancellationToken) =>
        Service().GetAsync(sessionId, cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<LineItem>> GetLineItemsAsync(string sessionId, CancellationToken cancellationToken)
    {
        var result = new List<LineItem>();
        var options = new SessionLineItemListOptions { Limit = 100, Expand = ["data.price.product"] };
        await foreach (var item in new SessionLineItemService(Client()).ListAutoPagingAsync(sessionId, options, cancellationToken: cancellationToken))
            result.Add(item);
        return result;
    }
}
