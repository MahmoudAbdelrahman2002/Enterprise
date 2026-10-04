using System.Net;
using System.Net.Http.Json;
using Enterprise.Application.Common.Authorization;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Admin.Services.DTOs;
using Enterprise.Application.Features.Provider.Categories.DTOs;
using Enterprise.Application.Features.Provider.Products.DTOs;
using Microsoft.Extensions.DependencyInjection;

namespace Enterprise.IntegrationTests;

[TestFixture]
public sealed class CatalogueEditingApiTests : TestFixtureBase
{
    private async Task<HttpClient> OwnerAsync()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/provider/auth/login", new
        {
            email = "provider@enterprise.local", password = "Provider@12345!"
        });
        response.EnsureSuccessStatusCode();
        return AuthenticatedClient((await ReadApiDataAsync<AuthResponse>(response))!.AccessToken);
    }

    private async Task<CategoryDetailDto> CategoryAsync(HttpClient owner, string name)
    {
        var response = await owner.PostAsJsonAsync("/api/v1/provider/categories", new { name = new { en = name }, isActive = true, displayOrder = 0 });
        response.EnsureSuccessStatusCode();
        return (await ReadApiDataAsync<CategoryDetailDto>(response))!;
    }

    [Test]
    public async Task ProductEditor_UpdatesAndMovesExistingRecordThroughPreservedRoutes()
    {
        using var owner = await OwnerAsync();
        var source = await CategoryAsync(owner, "Editor source");
        var destination = await CategoryAsync(owner, "Editor destination");
        var sourceRoute = $"/api/v1/provider/categories/{source.Id}/products";
        var create = await owner.PostAsJsonAsync(sourceRoute, new { name = new { en = "Original product", it = "Nome", ar = "اسم" }, sku = UniqueSku(), price = 10, status = 1 });
        create.EnsureSuccessStatusCode();
        var original = (await ReadApiDataAsync<ProductDetailDto>(create))!;
        var update = await owner.PutAsJsonAsync($"{sourceRoute}/{original.Id}", new
        {
            categoryId = destination.Id, name = new { en = "Updated product", it = "Nome", ar = "اسم" },
            description = new { en = "Updated description", it = "Descrizione", ar = "وصف" },
            sku = original.Sku, price = 15.25m, status = 2
        });
        update.EnsureSuccessStatusCode();
        var savedResponse = await owner.GetAsync($"/api/v1/provider/categories/{destination.Id}/products/{original.Id}");
        savedResponse.EnsureSuccessStatusCode();
        var saved = (await ReadApiDataAsync<ProductDetailDto>(savedResponse))!;
        Assert.Multiple(() =>
        {
            Assert.That(saved.Id, Is.EqualTo(original.Id));
            Assert.That(saved.CategoryId, Is.EqualTo(destination.Id));
            Assert.That(saved.Price, Is.EqualTo(15.25m));
            Assert.That((int)saved.Status, Is.EqualTo(2));
            Assert.That(saved.Translations!.Name.It, Is.EqualTo("Nome"));
            Assert.That(saved.Translations.Description!.Ar, Is.EqualTo("وصف"));
        });
        Assert.That((await owner.GetAsync($"{sourceRoute}/{original.Id}")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await owner.GetAsync("/api/v1/provider/products")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task CategoryEditor_PersistsDetailsAndRejectsReadOnlyStaff()
    {
        using var owner = await OwnerAsync();
        var category = await CategoryAsync(owner, "Editable category");
        var route = $"/api/v1/provider/categories/{category.Id}";
        var body = new { name = new { en = "Updated category", it = "Categoria", ar = "فئة" }, description = new { en = "Updated description" }, displayOrder = 4 };
        (await owner.PutAsJsonAsync(route, body)).EnsureSuccessStatusCode();
        var saved = (await ReadApiDataAsync<CategoryDetailDto>(await owner.GetAsync(route)))!;
        Assert.That(saved.DisplayOrder, Is.EqualTo(4));
        Assert.That(saved.Translations!.Name.It, Is.EqualTo("Categoria"));

        using var scope = Factory.Services.CreateScope();
        var account = await scope.ServiceProvider.GetRequiredService<IUserAccountService>().FindByEmailAsync("cashier@demo-restaurant.local");
        var token = scope.ServiceProvider.GetRequiredService<ITokenService>().GenerateAccessToken(account! with { Permissions = [Permissions.ProviderCategory.Read] });
        using var staff = AuthenticatedClient(token.Token);
        Assert.That((await staff.PutAsJsonAsync(route, body)).StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task ServiceEditor_PersistsDetailsWithoutReplacingRecord()
    {
        using var admin = AuthenticatedClient((await LoginAsAdminAsync()).AccessToken);
        var code = "editor-" + Guid.NewGuid().ToString("N");
        var create = await admin.PostAsJsonAsync("/api/v1/admin/services", new { code, name = new { en = "Editor service" }, isActive = true, displayOrder = 0 });
        create.EnsureSuccessStatusCode();
        var original = (await ReadApiDataAsync<MarketplaceServiceDto>(create))!;
        var route = $"/api/v1/admin/services/{original.Id}";
        (await admin.PutAsJsonAsync(route, new { code, name = new { en = "Updated service", it = "Servizio", ar = "خدمة" }, description = new { en = "Service description" }, displayOrder = 3 })).EnsureSuccessStatusCode();
        var saved = (await ReadApiDataAsync<MarketplaceServiceDto>(await admin.GetAsync(route)))!;
        Assert.Multiple(() =>
        {
            Assert.That(saved.Id, Is.EqualTo(original.Id));
            Assert.That(saved.DisplayOrder, Is.EqualTo(3));
            Assert.That(saved.Translations.Name.It, Is.EqualTo("Servizio"));
            Assert.That(saved.Translations.Description!.En, Is.EqualTo("Service description"));
            Assert.That(saved.IsActive, Is.True);
        });
    }
}
