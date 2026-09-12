using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Enterprise.IntegrationTests;

[TestFixture]
public class ProductsEndpointsTests : TestFixtureBase
{
    [Test]
    public async Task GetList_WithoutAuthentication_ReturnsOk()
    {
        var response = await Client.GetAsync("/api/v1/products");

        // Products list currently has no [RequirePermission] (Phase 1 leaves products as-is).
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetList_AsRegisteredUser_ReturnsSeededProducts()
    {
        var registration = await RegisterAsync();
        using var client = AuthenticatedClient(registration.AccessToken);

        var response = await client.GetAsync("/api/v1/products?pageNumber=1&pageSize=50");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await ReadApiDataAsync<PagedResultDto<ProductDto>>(response);
        page!.Items.Should().NotBeEmpty();
        page.TotalCount.Should().BeGreaterThanOrEqualTo(page.Items.Count);
    }

    [Test]
    public async Task Create_AsRegisteredUser_ReturnsForbidden()
    {
        var registration = await RegisterAsync();
        using var client = AuthenticatedClient(registration.AccessToken);

        var response = await client.PostAsJsonAsync("/api/v1/products", new
        {
            sku = UniqueSku(),
            name = "Test product",
            description = "desc",
            category = "Test",
            price = 9.99m,
            stockQuantity = 5
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Create_AsAdmin_ReturnsCreatedWithLocationHeader()
    {
        var admin = await LoginAsAdminAsync();
        using var client = AuthenticatedClient(admin.AccessToken);
        var sku = UniqueSku();

        var response = await client.PostAsJsonAsync("/api/v1/products", new
        {
            sku,
            name = "Integration test product",
            description = "Created by an integration test",
            category = "Test",
            price = 19.99m,
            stockQuantity = 3
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var created = await ReadApiDataAsync<ProductDto>(response);
        created!.Sku.Should().Be(sku);
    }

    [Test]
    public async Task Create_DuplicateSku_ReturnsConflict()
    {
        var admin = await LoginAsAdminAsync();
        using var client = AuthenticatedClient(admin.AccessToken);
        var sku = UniqueSku();
        var payload = new
        {
            sku,
            name = "Duplicate SKU product",
            description = (string?)null,
            category = "Test",
            price = 5m,
            stockQuantity = 1
        };

        var first = await client.PostAsJsonAsync("/api/v1/products", payload);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("/api/v1/products", payload);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var envelope = await ReadApiResponseAsync<object>(second);
        envelope!.Success.Should().BeFalse();
    }

    [Test]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var registration = await RegisterAsync();
        using var client = AuthenticatedClient(registration.AccessToken);

        var response = await client.GetAsync($"/api/v1/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task AdjustStock_DecreaseBelowZero_ReturnsConflict()
    {
        var admin = await LoginAsAdminAsync();
        using var client = AuthenticatedClient(admin.AccessToken);

        var createResponse = await client.PostAsJsonAsync("/api/v1/products", new
        {
            sku = UniqueSku(),
            name = "Low stock product",
            description = (string?)null,
            category = "Test",
            price = 5m,
            stockQuantity = 2
        });
        var created = await ReadApiDataAsync<ProductDto>(createResponse);

        var response = await client.PostAsJsonAsync($"/api/v1/products/{created!.Id}/adjust-stock", new { delta = -10 });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task DeleteThenGetById_AsAdmin_ReturnsOkThenNotFound()
    {
        var admin = await LoginAsAdminAsync();
        using var client = AuthenticatedClient(admin.AccessToken);

        var createResponse = await client.PostAsJsonAsync("/api/v1/products", new
        {
            sku = UniqueSku(),
            name = "To be deleted",
            description = (string?)null,
            category = "Test",
            price = 5m,
            stockQuantity = 2
        });
        var created = await ReadApiDataAsync<ProductDto>(createResponse);

        var deleteResponse = await client.DeleteAsync($"/api/v1/products/{created!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await client.GetAsync($"/api/v1/products/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

public sealed record PagedResultDto<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount);

public sealed record ProductDto(
    Guid Id, string Sku, string Name, string? Description, string Category,
    decimal Price, int StockQuantity, int Status, DateTime CreatedAtUtc);
