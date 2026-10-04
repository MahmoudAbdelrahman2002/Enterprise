using Stripe;
using Stripe.Checkout;

namespace Enterprise.Application.Features.Client.Payments;

public interface ICheckoutGateway
{
    Task<Session> CreateAsync(SessionCreateOptions options, string idempotencyKey, CancellationToken cancellationToken);
    Task<Session> GetAsync(string sessionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<LineItem>> GetLineItemsAsync(string sessionId, CancellationToken cancellationToken);
}
