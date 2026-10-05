using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Enterprise.Application.Common.Settings;
using Enterprise.Application.Features.Client.Payments;
using Enterprise.Application.Features.Client.Payments.Commands.CreatePaymentCommand;
using Enterprise.Application.Features.Orders.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stripe;
using Stripe.Checkout;

namespace Enterprise.IntegrationTests;

[TestFixture]
public sealed class CheckoutLifecycleTests : TestFixtureBase
{
    private const string WebhookSecret = "whsec_isolated_checkout_regression";
    private readonly TestCheckoutGateway _gateway = new();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.RemoveAll<ICheckoutGateway>();
        services.AddSingleton<ICheckoutGateway>(_gateway);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new StripeSettings
        {
            SecretKey = "sk_test_isolated_fake_gateway",
            WebhookSecret = WebhookSecret,
            SuccessUrl = "http://localhost/payment/success?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = "http://localhost/payment/cancel"
        }));
    }

    private async Task<(HttpClient Client, Guid ProviderId, Guid ProductId, Guid CartId)> BasketAsync()
    {
        var auth = await RegisterClientAsync();
        var client = AuthenticatedClient(auth.AccessToken);
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var product = await db.Products.Include(p => p.Category).FirstAsync(p => p.Status == Enterprise.Domain.Enums.ProductStatus.Active && p.Category.IsActive);
        var cart = new ShoppingCart { UserId = auth.User.Id, ProviderId = product.Category.ProviderId };
        cart.Items.Add(new ShoppingCartItem { Id = Guid.NewGuid(), ProductId = product.Id, Quantity = 2 });
        db.ShoppingCarts.Add(cart);
        await db.SaveChangesAsync();
        return (client, cart.ProviderId, product.Id, cart.Id);
    }

    private async Task<Session> CheckoutAsync(HttpClient client, Guid providerId)
    {
        var response = await client.PostAsync($"/api/v1/client/{providerId}/payments", null);
        response.EnsureSuccessStatusCode();
        var data = (await ReadApiDataAsync<CreatePaymentSessionDto>(response))!;
        return await _gateway.GetAsync(data.SessionId, default);
    }

    private async Task<HttpResponseMessage> WebhookAsync(Session session, string type, bool validSignature = true)
    {
        var json = JsonSerializer.Serialize(new
        {
            id = "evt_" + Guid.NewGuid().ToString("N"), @object = "event", type,
            data = new { @object = new { id = session.Id, @object = "checkout.session", payment_status = session.PaymentStatus, amount_total = session.AmountTotal, metadata = session.Metadata } }
        });
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(WebhookSecret), Encoding.UTF8.GetBytes($"{timestamp}.{json}"))).ToLowerInvariant();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/stripe") { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        request.Headers.Add("Stripe-Signature", $"t={timestamp},v1={(validSignature ? signature : new string('0', 64))}");
        return await Client.SendAsync(request);
    }

    [Test]
    public async Task PaidCheckout_RetriesOnce_PreservesPaidPriceAndNewBasketQuantity_ThenFulfilsAndNotifies()
    {
        var basket = await BasketAsync();
        using var client = basket.Client;
        var session = await CheckoutAsync(client, basket.ProviderId);
        Assert.That((await CheckoutAsync(client, basket.ProviderId)).Id, Is.EqualTo(session.Id));
        var paidLine = (await _gateway.GetLineItemsAsync(session.Id, default)).Single();
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var product = await db.Products.SingleAsync(p => p.Id == basket.ProductId);
            product.UpdateDetails(product.Sku, product.Price + 1);
            var item = await db.ShoppingCartItems.SingleAsync(i => i.ShoppingCartId == basket.CartId);
            item.Quantity = 3;
            await db.SaveChangesAsync();
        }
        session.PaymentStatus = "paid";
        (await WebhookAsync(session, EventTypes.CheckoutSessionCompleted)).EnsureSuccessStatusCode();
        (await WebhookAsync(session, EventTypes.CheckoutSessionCompleted)).EnsureSuccessStatusCode();
        var confirmed = await client.PostAsync($"/api/v1/client/orders/confirm-session/{session.Id}", null);
        confirmed.EnsureSuccessStatusCode();
        var order = (await ReadApiDataAsync<OrderDetailDto>(confirmed))!;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var saved = await db.Orders.Include(o => o.OrderItems).SingleAsync(o => o.StripeCheckoutSessionId == session.Id);
            Assert.Multiple(() =>
            {
                Assert.That(saved.OrderItems.Single().UnitPrice, Is.EqualTo(paidLine.Price.UnitAmount / 100m));
                Assert.That(saved.OrderItems.Single().ProductName, Is.EqualTo(paidLine.Description));
                Assert.That(saved.OrderItems.Single().Quantity, Is.EqualTo(2));
                Assert.That(saved.TotalAmount, Is.EqualTo(session.AmountTotal / 100m));
            });
            Assert.That(await db.ShoppingCartItems.Where(i => i.ShoppingCartId == basket.CartId).Select(i => i.Quantity).SingleAsync(), Is.EqualTo(1));
            Assert.That(await db.Notifications.CountAsync(n => n.RelatedEntityId == order.Id && n.NotificationType == "new_order"), Is.GreaterThan(0));
            var recipients = await db.Notifications.Where(n => n.RelatedEntityId == order.Id && n.NotificationType == "new_order").Select(n => n.RecipientUserId).ToListAsync();
            Assert.That(recipients.Distinct().Count(), Is.EqualTo(recipients.Count), "Repeated webhooks must not notify the same recipient twice.");
        }
        var login = await Client.PostAsJsonAsync("/api/v1/provider/auth/login", new { email = "provider@enterprise.local", password = "Provider@12345!" });
        login.EnsureSuccessStatusCode();
        using var owner = AuthenticatedClient((await ReadApiDataAsync<AuthResponse>(login))!.AccessToken);
        Assert.That(order.Status, Is.EqualTo(OrderStatus.New));
        Assert.That((await owner.PatchAsJsonAsync($"/api/v1/provider/orders/{order.Id}/status", new { status = 3 })).StatusCode, Is.EqualTo(HttpStatusCode.Conflict), "New orders must enter Preparing before Ready.");
        foreach (var status in new[] { 1, 4, 5 })
            Assert.That((await owner.PatchAsJsonAsync($"/api/v1/provider/orders/{order.Id}/status", new { status })).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Removed statuses are rejected.");
        Assert.That((await client.PostAsync($"/api/v1/client/orders/{order.Id}/cancel", null)).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        foreach (var status in new[] { 2, 3 })
            (await owner.PatchAsJsonAsync($"/api/v1/provider/orders/{order.Id}/status", new { status })).EnsureSuccessStatusCode();
        Assert.That((await owner.PatchAsJsonAsync($"/api/v1/provider/orders/{order.Id}/status", new { status = 0 })).StatusCode, Is.EqualTo(HttpStatusCode.Conflict), "Ready is terminal.");
        using var finalScope = Factory.Services.CreateScope();
        var finalDb = finalScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.That((await finalDb.Orders.SingleAsync(o => o.Id == order.Id)).Status, Is.EqualTo(OrderStatus.Ready));
        Assert.That(await finalDb.Notifications.CountAsync(n => n.RelatedEntityId == order.Id && n.RecipientUserType == Enterprise.Domain.Enums.UserType.Client), Is.EqualTo(2));
    }

    [Test]
    public async Task HistoricalCancelledOrder_RemainsReadableButOutsideTheActiveProviderWorkflow()
    {
        var basket = await BasketAsync();
        using var client = basket.Client;
        var orderId = Guid.Empty;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userId = await db.ShoppingCarts.Where(c => c.Id == basket.CartId).Select(c => c.UserId).SingleAsync();
            var historicalOrder = new Order { UserId = userId, ProviderId = basket.ProviderId, OrderDateUtc = DateTime.UtcNow, Status = OrderStatus.Ready, PreviousStatus = "Cancelled", TotalAmount = 1m };
            db.Orders.Add(historicalOrder);
            await db.SaveChangesAsync();
            orderId = historicalOrder.Id;
        }
        var detail = await client.GetAsync($"/api/v1/client/orders/{orderId}");
        detail.EnsureSuccessStatusCode();
        Assert.That((await ReadApiDataAsync<OrderDetailDto>(detail))!.IsHistorical, Is.True);
        var login = await Client.PostAsJsonAsync("/api/v1/provider/auth/login", new { email = "provider@enterprise.local", password = "Provider@12345!" });
        login.EnsureSuccessStatusCode();
        using var owner = AuthenticatedClient((await ReadApiDataAsync<AuthResponse>(login))!.AccessToken);
        var list = await owner.GetAsync("/api/v1/provider/orders?pageNumber=1&pageSize=100");
        list.EnsureSuccessStatusCode();
        var page = (await ReadApiDataAsync<Enterprise.Application.Common.Models.PagedResult<OrderListItemDto>>(list))!;
        Assert.That(page.Items.Any(item => item.Id == orderId), Is.False);
        var providerDetail = await owner.GetAsync($"/api/v1/provider/orders/{orderId}");
        providerDetail.EnsureSuccessStatusCode();
        Assert.That((await ReadApiDataAsync<OrderDetailDto>(providerDetail))!.IsHistorical, Is.True);
        Assert.That((await owner.PatchAsJsonAsync($"/api/v1/provider/orders/{orderId}/status", new { status = 2 })).StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task UnpaidOrForgedSession_DoesNotCreateOrder_AndAsyncSuccessConsumesBasket()
    {
        var basket = await BasketAsync();
        using var client = basket.Client;
        var session = await CheckoutAsync(client, basket.ProviderId);
        Assert.That((await client.PostAsync($"/api/v1/client/orders/confirm-session/{session.Id}", null)).StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        (await WebhookAsync(session, EventTypes.CheckoutSessionCompleted)).EnsureSuccessStatusCode();
        session.PaymentStatus = "paid";
        Assert.That((await WebhookAsync(session, EventTypes.CheckoutSessionCompleted, false)).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.That(await db.Orders.AnyAsync(o => o.StripeCheckoutSessionId == session.Id), Is.False);
            Assert.That(await db.ShoppingCarts.AnyAsync(c => c.Id == basket.CartId), Is.True);
        }
        using var other = AuthenticatedClient((await RegisterClientAsync()).AccessToken);
        Assert.That((await other.PostAsync($"/api/v1/client/orders/confirm-session/{session.Id}", null)).StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        (await WebhookAsync(session, EventTypes.CheckoutSessionAsyncPaymentSucceeded)).EnsureSuccessStatusCode();
        using var finalScope = Factory.Services.CreateScope();
        var finalDb = finalScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.That(await finalDb.Orders.CountAsync(o => o.StripeCheckoutSessionId == session.Id), Is.EqualTo(1));
        Assert.That(await finalDb.ShoppingCarts.AnyAsync(c => c.Id == basket.CartId), Is.False);
    }

    [Test]
    public async Task VerificationPolicy_ReportsConfiguredExpiryAndThirtySecondResendWait()
    {
        var response = await Client.GetAsync("/api/v1/auth/verification-policy");
        response.EnsureSuccessStatusCode();
        var policy = (await ReadApiDataAsync<JsonElement>(response));
        Assert.Multiple(() =>
        {
            Assert.That(policy.GetProperty("expirationMinutes").GetInt32(), Is.EqualTo(10));
            Assert.That(policy.GetProperty("maxAttempts").GetInt32(), Is.EqualTo(5));
            Assert.That(policy.GetProperty("resendWaitSeconds").GetInt32(), Is.EqualTo(30));
        });
    }

    [Test]
    public async Task GatewayFailure_ReturnsUseful503_PreservesBasket_AndAllowsRetry()
    {
        var basket = await BasketAsync();
        using var client = basket.Client;
        _gateway.Unavailable = true;
        try
        {
            var response = await client.PostAsync($"/api/v1/client/{basket.ProviderId}/payments", null);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
            var body = await ReadApiResponseAsync<JsonElement>(response);
            Assert.That(body!.Message, Does.Contain("basket"));
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.That(await db.ShoppingCartItems.Where(i => i.ShoppingCartId == basket.CartId).Select(i => i.Quantity).SingleAsync(), Is.EqualTo(2));
        }
        finally { _gateway.Unavailable = false; }
        Assert.That((await CheckoutAsync(client, basket.ProviderId)).Id, Does.StartWith("cs_test_"));
    }

    [Test]
    public async Task ExpiredRegistrationCode_IsRejected_AndResendAllowsCompletion()
    {
        var email = UniqueEmail();
        (await Client.PostAsJsonAsync("/api/v1/client/auth/register", new { email, firstName = "Expiry", lastName = "Check" })).EnsureSuccessStatusCode();
        var oldCode = CapturingEmailSender.CapturedOtps[email];
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.OtpChallenges.Where(o => o.Email == email).ExecuteUpdateAsync(update => update.SetProperty(o => o.ExpiresAtUtc, DateTime.UtcNow.AddMinutes(-1)));
        }
        var expired = await Client.PostAsJsonAsync("/api/v1/client/auth/verify-registration", new { email, otp = oldCode });
        Assert.That(expired.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        (await Client.PostAsJsonAsync("/api/v1/client/auth/login", new { email })).EnsureSuccessStatusCode();
        var newCode = CapturingEmailSender.CapturedOtps[email];
        (await Client.PostAsJsonAsync("/api/v1/client/auth/verify-registration", new { email, otp = newCode })).EnsureSuccessStatusCode();
    }

    private sealed class TestCheckoutGateway : ICheckoutGateway
    {
        public bool Unavailable { get; set; }
        private readonly Dictionary<string, Session> _sessions = new();
        private readonly Dictionary<string, IReadOnlyList<LineItem>> _lines = new();
        public Task<Session> CreateAsync(SessionCreateOptions options, string key, CancellationToken ct)
        {
            if (Unavailable) throw new Enterprise.Application.Common.Exceptions.PaymentUnavailableException();
            if (_sessions.TryGetValue(key, out var existing)) return Task.FromResult(existing);
            var session = new Session { Id = "cs_test_" + Guid.NewGuid().ToString("N"), Url = "https://checkout.stripe.com/test", PaymentStatus = "unpaid", Metadata = new(options.Metadata) };
            var lines = options.LineItems.Select(item => new LineItem { Quantity = item.Quantity, Description = item.PriceData.ProductData.Name,
                Price = new Price { UnitAmount = item.PriceData.UnitAmount, Product = new Stripe.Product { Name = item.PriceData.ProductData.Name, Metadata = new(item.PriceData.ProductData.Metadata) } } }).ToArray();
            session.AmountTotal = lines.Sum(line => line.Price.UnitAmount!.Value * line.Quantity!.Value);
            _sessions[key] = session; _sessions[session.Id] = session; _lines[session.Id] = lines;
            return Task.FromResult(session);
        }
        public Task<Session> GetAsync(string id, CancellationToken ct) => Task.FromResult(_sessions[id]);
        public Task<IReadOnlyList<LineItem>> GetLineItemsAsync(string id, CancellationToken ct) => Task.FromResult(_lines[id]);
    }
}
