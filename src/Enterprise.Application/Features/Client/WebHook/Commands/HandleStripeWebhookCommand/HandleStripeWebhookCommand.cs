using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Settings;
using Enterprise.Application.Features.Client.Payments;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Enterprise.Application.Features.Client.WebHook.Commands.HandleStripeWebhookCommand;

public sealed class HandleStripeWebhookCommand(string json, string signature) : IRequest
{
    public string Json { get; } = json;
    public string Signature { get; } = signature;
}

public sealed class HandleStripeWebhookCommandHandler(
    ICheckoutOrderService checkoutOrderService,
    IOptions<StripeSettings> stripeOptions,
    ILogger<HandleStripeWebhookCommandHandler> logger) : IRequestHandler<HandleStripeWebhookCommand>
{
    public async Task Handle(HandleStripeWebhookCommand request, CancellationToken cancellationToken)
    {
        var webhookSecret = stripeOptions.Value.WebhookSecret;
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            throw new InvalidOperationException("Stripe:WebhookSecret is not configured.");
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                request.Json,
                request.Signature,
                webhookSecret,
                tolerance: 300,
                throwOnApiVersionMismatch: false);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Stripe webhook signature verification failed");
            throw;
        }

        logger.LogInformation("Stripe webhook received: {EventType} ({EventId})", stripeEvent.Type, stripeEvent.Id);

        switch (stripeEvent.Type)
        {
            case EventTypes.CheckoutSessionCompleted:
            {
                var session = stripeEvent.Data.Object as Session
                    ?? throw new InvalidOperationException("checkout.session.completed payload missing Session.");

                LogMetadata("checkout.session", session.Id, session.Metadata);
                await checkoutOrderService.CompletePaidCheckoutAsync(session, cancellationToken);
                break;
            }
            case EventTypes.PaymentIntentSucceeded:
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent
                    ?? throw new InvalidOperationException("payment_intent.succeeded payload missing PaymentIntent.");
                LogMetadata("payment_intent", paymentIntent.Id, paymentIntent.Metadata);
                break;
            }
            case EventTypes.ChargeSucceeded:
            {
                var charge = stripeEvent.Data.Object as Charge
                    ?? throw new InvalidOperationException("charge.succeeded payload missing Charge.");
                LogMetadata("charge", charge.Id, charge.Metadata);
                break;
            }
            default:
                logger.LogDebug("Ignoring Stripe event type {EventType}", stripeEvent.Type);
                break;
        }
    }

    private void LogMetadata(string source, string id, IDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            logger.LogWarning("Stripe {Source} {Id} has empty metadata", source, id);
            return;
        }

        logger.LogInformation(
            "Stripe {Source} {Id} metadata: {Metadata}",
            source,
            id,
            string.Join(", ", metadata.Select(kv => $"{kv.Key}={kv.Value}")));
    }
}
