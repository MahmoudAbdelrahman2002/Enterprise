using System.Net.Http.Json;
using Enterprise.Application.Common.Settings;
using Enterprise.Application.Features.Client.Payments.Commands.CreatePaymentCommand;
using Enterprise.Domain.Entities;
using Enterprise.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Enterprise.IntegrationTests;

[TestFixture]
public sealed class StripeTestCheckoutTests : TestFixtureBase
{
    [Test, Explicit("Requires an existing Stripe test credential and network access; creates then expires an unpaid test checkout.")]
    public async Task RealTestGateway_CreatesHostedSession_ReusesRetry_AndExpandsProductMetadata()
    {
        using var scope = Factory.Services.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<IOptions<StripeSettings>>().Value;
        Assert.That(settings.SecretKey.StartsWith("sk_test_", StringComparison.Ordinal), Is.True, "Only a test-mode credential may be used.");
        var auth = await RegisterClientAsync();
        using var client = AuthenticatedClient(auth.AccessToken);
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var product = await db.Products.Include(p => p.Category).FirstAsync(p => p.Status == Enterprise.Domain.Enums.ProductStatus.Active && p.Category.IsActive);
        (await client.PostAsJsonAsync($"/api/v1/client/{product.Category.ProviderId}/cart/items", new { productId = product.Id, quantity = 2 })).EnsureSuccessStatusCode();
        var sessions = new SessionService(new StripeClient(settings.SecretKey));
        string? sessionId = null;
        try
        {
            var response = await client.PostAsync($"/api/v1/client/{product.Category.ProviderId}/payments", null);
            response.EnsureSuccessStatusCode();
            var created = (await ReadApiDataAsync<CreatePaymentSessionDto>(response))!;
            sessionId = created.SessionId;
            Assert.That(created.Url, Does.StartWith("https://checkout.stripe.com/"));
            var session = await sessions.GetAsync(sessionId);
            Assert.That(session.Livemode, Is.False);
            var retry = await client.PostAsync($"/api/v1/client/{product.Category.ProviderId}/payments", null);
            retry.EnsureSuccessStatusCode();
            Assert.That((await ReadApiDataAsync<CreatePaymentSessionDto>(retry))!.SessionId, Is.EqualTo(sessionId));
            var gateway = scope.ServiceProvider.GetRequiredService<Enterprise.Application.Features.Client.Payments.ICheckoutGateway>();
            var lines = await gateway.GetLineItemsAsync(sessionId, default);
            Assert.That(lines.Single().Price.Product.Metadata["productId"], Is.EqualTo(product.Id.ToString()));
            Assert.That(lines.Single().Quantity, Is.EqualTo(2));
        }
        finally
        {
            if (sessionId is not null) Assert.That((await sessions.ExpireAsync(sessionId)).Status, Is.EqualTo("expired"));
        }
    }
}
