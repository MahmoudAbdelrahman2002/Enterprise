using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Enums;
using Enterprise.Infrastructure.Identity;
using Enterprise.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Enterprise.IntegrationTests;

[TestFixture]
public sealed class PaginationApiTests : TestFixtureBase
{
    private AuthResponse? _client;
    private AuthResponse? _otherClient;
    private async Task<AuthResponse> ClientAuthAsync() => _client ??= await RegisterClientAsync();
    private async Task<AuthResponse> OtherClientAuthAsync() => _otherClient ??= await RegisterClientAsync();

    private static async Task<JsonElement> PageAsync(HttpClient client, string route)
    {
        var response = await client.GetAsync(route);
        response.EnsureSuccessStatusCode();
        return await ReadApiDataAsync<JsonElement>(response);
    }

    [TestCase("admin")]
    [TestCase("provider")]
    [TestCase("client")]
    public async Task NotificationPages_AreScopedStableAndReadOnlyDisplayedNotifications(string portal)
    {
        AuthResponse auth;
        if (portal == "client") auth = await ClientAuthAsync();
        else
        {
            var response = await Client.PostAsJsonAsync($"/api/v1/{portal}/auth/login", new { email = $"{portal}@enterprise.local", password = portal == "admin" ? "Admin@12345!" : "Provider@12345!" });
            response.EnsureSuccessStatusCode(); auth = (await ReadApiDataAsync<AuthResponse>(response))!;
        }
        using var client = AuthenticatedClient(auth.AccessToken);
        var userType = Enum.Parse<UserType>(portal, true);
        await OtherClientAuthAsync();
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Notifications.Where(n => n.RecipientUserId == auth.User.Id && n.RecipientUserType == userType).ExecuteDeleteAsync();
        var notifications = Enumerable.Range(0, 25).Select(index => new Notification(auth.User.Id, userType, $"Notice {index}", "Pagination check", "test_notice")).ToArray();
        db.Notifications.AddRange(notifications);
        var otherRecipientId = await db.Users.Where(user => user.UserType == userType && user.Id != auth.User.Id).Select(user => user.Id).FirstAsync();
        var other = new Notification(otherRecipientId, userType, "Other recipient", "Must remain unread", "test_notice");
        db.Notifications.Add(other); await db.SaveChangesAsync();
        var first = await PageAsync(client, $"/api/v1/{portal}/notifications/paged?pageSize=20&pageNumber=1");
        Assert.Multiple(() =>
        {
            Assert.That(first.GetProperty("totalCount").GetInt32(), Is.EqualTo(25));
            Assert.That(first.GetProperty("totalPages").GetInt32(), Is.EqualTo(2));
            Assert.That(first.GetProperty("items").GetArrayLength(), Is.EqualTo(20));
            Assert.That(first.GetProperty("items").EnumerateArray().All(n => n.GetProperty("isRead").GetBoolean()), Is.True);
        });
        Assert.That(await db.Notifications.CountAsync(n => n.RecipientUserId == auth.User.Id && n.RecipientUserType == userType && !n.IsRead), Is.EqualTo(5));
        Assert.That(await db.Notifications.Where(n => n.Id == other.Id).Select(n => n.IsRead).SingleAsync(), Is.False);
        var second = await PageAsync(client, $"/api/v1/{portal}/notifications/paged?pageSize=20&pageNumber=2");
        Assert.That(second.GetProperty("items").GetArrayLength(), Is.EqualTo(5));
        var ids = first.GetProperty("items").EnumerateArray().Concat(second.GetProperty("items").EnumerateArray()).Select(n => n.GetProperty("id").GetGuid()).ToArray();
        Assert.That(ids.Distinct().Count(), Is.EqualTo(25));
        var repeated = await PageAsync(client, $"/api/v1/{portal}/notifications/paged?pageSize=20&pageNumber=1");
        Assert.That(repeated.GetProperty("items").EnumerateArray().Select(n => n.GetProperty("id").GetGuid()), Is.EqualTo(ids.Take(20)));
        var legacy = await PageAsync(client, $"/api/v1/{portal}/notifications");
        Assert.That(legacy.ValueKind, Is.EqualTo(JsonValueKind.Array));
    }

    [Test]
    public async Task BasketPages_KeepFullSubtotalsAndAggregateQuantity_AndExcludeOtherUsersAndEmptyBaskets()
    {
        var auth = await ClientAuthAsync();
        using var client = AuthenticatedClient(auth.AccessToken);
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        for (var index = 0; index < 13; index++)
        {
            var user = new ApplicationUser { Id = Guid.NewGuid(), UserType = UserType.Provider, UserName = UniqueEmail() };
            db.Users.Add(user);
            var provider = new Provider(user.Id, $"Paging Store {index}");
            var category = new Category(provider.Id); category.UpsertTranslation("en", "Paging category");
            var product = new Product(category.Id, $"PAGING-{index}", 10, ProductStatus.Active); product.UpsertTranslation("en", "Paging product");
            var cart = new ShoppingCart { UserId = auth.User.Id, ProviderId = provider.Id };
            cart.Items.Add(new ShoppingCartItem { Id = Guid.NewGuid(), ProductId = product.Id, Quantity = 2 });
            db.Providers.Add(provider); db.Categories.Add(category); db.Products.Add(product); db.ShoppingCarts.Add(cart);
        }
        var emptyOwner = new ApplicationUser { Id = Guid.NewGuid(), UserType = UserType.Provider, UserName = UniqueEmail() }; db.Users.Add(emptyOwner);
        var emptyProvider = new Provider(emptyOwner.Id, "Empty basket store"); db.Providers.Add(emptyProvider);
        db.ShoppingCarts.Add(new ShoppingCart { UserId = auth.User.Id, ProviderId = emptyProvider.Id });
        await db.SaveChangesAsync();
        var first = await PageAsync(client, "/api/v1/client/carts/paged?pageNumber=1&pageSize=12");
        var second = await PageAsync(client, "/api/v1/client/carts/paged?pageNumber=2&pageSize=12");
        Assert.That(first.GetProperty("totalCount").GetInt32(), Is.EqualTo(13));
        Assert.That(first.GetProperty("items").GetArrayLength(), Is.EqualTo(12));
        Assert.That(second.GetProperty("items").GetArrayLength(), Is.EqualTo(1));
        Assert.That(first.GetProperty("items").EnumerateArray().All(cart => cart.GetProperty("totalPrice").GetDecimal() == 20), Is.True);
        var ids = first.GetProperty("items").EnumerateArray().Concat(second.GetProperty("items").EnumerateArray()).Select(cart => cart.GetProperty("id").GetGuid());
        Assert.That(ids.Distinct().Count(), Is.EqualTo(13));
        Assert.That((await PageAsync(client, "/api/v1/client/carts/count")).GetProperty("totalQuantity").GetInt32(), Is.EqualTo(26));
        Assert.That((await PageAsync(client, "/api/v1/client/carts")).ValueKind, Is.EqualTo(JsonValueKind.Array));
        using var other = AuthenticatedClient((await OtherClientAuthAsync()).AccessToken);
        Assert.That((await PageAsync(other, "/api/v1/client/carts/paged")).GetProperty("totalCount").GetInt32(), Is.Zero);
        Assert.That((await PageAsync(other, "/api/v1/client/carts/count")).GetProperty("totalQuantity").GetInt32(), Is.Zero);
    }

    [Test]
    public async Task ClientOrderPages_AreActuallyPagedInTheDatabase_AndRemainPrivate()
    {
        var auth = await ClientAuthAsync(); using var client = AuthenticatedClient(auth.AccessToken);
        using var scope = Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var providerId = await db.Providers.Select(p => p.Id).FirstAsync();
        for (var index = 0; index < 21; index++)
            db.Orders.Add(new Order { UserId = auth.User.Id, ProviderId = providerId, OrderDateUtc = new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc), TotalAmount = index + 1, Status = OrderStatus.New });
        await db.SaveChangesAsync();
        var pages = new List<JsonElement>();
        for (var page = 1; page <= 3; page++) pages.Add(await PageAsync(client, $"/api/v1/client/orders/paged?pageNumber={page}&pageSize=10"));
        Assert.That(pages.Select(page => page.GetProperty("items").GetArrayLength()), Is.EqualTo(new[] { 10, 10, 1 }));
        Assert.That(pages[0].GetProperty("totalCount").GetInt32(), Is.EqualTo(21));
        Assert.That(pages.SelectMany(page => page.GetProperty("items").EnumerateArray()).Select(order => order.GetProperty("id").GetGuid()).Distinct().Count(), Is.EqualTo(21));
        var filtered = await PageAsync(client, $"/api/v1/client/orders/paged?providerId={Guid.NewGuid()}");
        Assert.That(filtered.GetProperty("totalCount").GetInt32(), Is.Zero);
        using var other = AuthenticatedClient((await OtherClientAuthAsync()).AccessToken);
        Assert.That((await PageAsync(other, "/api/v1/client/orders/paged")).GetProperty("totalCount").GetInt32(), Is.Zero);
        Assert.That((await PageAsync(client, "/api/v1/client/orders")).ValueKind, Is.EqualTo(JsonValueKind.Array));
    }

    [Test]
    public async Task StoreCategoriesAndProducts_CoverAllMatchesWithoutDuplicates_WhenSortKeysAreEqual()
    {
        using var scope = Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var providerId = await db.Providers.Where(p => db.Users.Any(u => u.Id == p.UserId && u.IsActive)).Select(p => p.Id).FirstAsync();
        var prefix = "Paging" + Guid.NewGuid().ToString("N")[..8];
        for (var index = 0; index < 25; index++)
        {
            var category = new Category(providerId, displayOrder: 0); category.UpsertTranslation("en", prefix + " category " + index);
            var product = new Product(category.Id, prefix, 1, ProductStatus.Active); product.UpsertTranslation("en", prefix + " product " + index);
            db.Categories.Add(category); db.Products.Add(product);
        }
        await db.SaveChangesAsync();
        foreach (var resource in new[] { "categories", "products" })
        {
            var allIds = new List<Guid>();
            for (var page = 1; page <= 3; page++)
            {
                var data = await PageAsync(Client, $"/api/v1/client/{providerId}/{resource}?searchTerm={prefix}&pageNumber={page}&pageSize=12");
                Assert.That(data.GetProperty("totalCount").GetInt32(), Is.EqualTo(25));
                allIds.AddRange(data.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid()));
            }
            Assert.That(allIds.Distinct().Count(), Is.EqualTo(25));
        }
    }

    [Test]
    public async Task MarketplaceServices_PublishAndPageBeyondTheFirstTwelve()
    {
        using var scope = Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var prefix = "paging-" + Guid.NewGuid().ToString("N")[..8];
        for (var index = 0; index < 25; index++)
        {
            var service = new MarketplaceService(prefix + "-" + index); service.UpsertTranslation("en", prefix + " " + index);
            var user = new ApplicationUser { Id = Guid.NewGuid(), UserType = UserType.Provider, IsActive = true, UserName = prefix + index + "@example.com", Email = prefix + index + "@example.com" };
            var provider = new Provider(user.Id, "Paged public store", serviceId: service.Id);
            db.MarketplaceServices.Add(service); db.Users.Add(user); db.Providers.Add(provider);
        }
        await db.SaveChangesAsync();
        var ids = new List<Guid>();
        for (var page = 1; page <= 3; page++)
        {
            var data = await PageAsync(Client, $"/api/v1/client/services?searchTerm={prefix}&pageNumber={page}&pageSize=12");
            Assert.That(data.GetProperty("totalCount").GetInt32(), Is.EqualTo(25));
            ids.AddRange(data.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid()));
        }
        Assert.That(ids.Distinct().Count(), Is.EqualTo(25));
    }

    [TestCase("notifications/paged", "pageSize=101")]
    [TestCase("notifications/paged", "pageNumber=-1")]
    [TestCase("carts/paged", "pageSize=0")]
    [TestCase("carts/paged", "pageNumber=2147483647&pageSize=100")]
    [TestCase("orders/paged", "pageNumber=0")]
    [TestCase("orders/paged", "pageNumber=1.5")]
    public async Task InvalidPagingParameters_AreRejected(string resource, string query)
    {
        using var client = AuthenticatedClient((await ClientAuthAsync()).AccessToken);
        Assert.That((await client.GetAsync($"/api/v1/client/{resource}?{query}")).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}
