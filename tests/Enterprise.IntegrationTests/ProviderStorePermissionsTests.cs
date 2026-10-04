using System.Net;
using System.Net.Http.Json;
using Enterprise.Application.Common.Authorization;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Provider.Store;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Enterprise.IntegrationTests;

[TestFixture]
public sealed class ProviderStorePermissionsTests : TestFixtureBase
{
    private async Task<HttpClient> StaffClientAsync(params string[] permissions)
    {
        using var scope = Factory.Services.CreateScope();
        var accounts = scope.ServiceProvider.GetRequiredService<IUserAccountService>();
        var staff = await accounts.FindByEmailAsync("cashier@demo-restaurant.local");
        staff.Should().NotBeNull();
        var token = scope.ServiceProvider.GetRequiredService<ITokenService>()
            .GenerateAccessToken(staff! with { Permissions = permissions });
        return AuthenticatedClient(token.Token);
    }

    private async Task<HttpClient> OwnerClientAsync()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/provider/auth/login", new
        {
            email = "provider@enterprise.local",
            password = "Provider@12345!"
        });
        response.EnsureSuccessStatusCode();
        return AuthenticatedClient((await ReadApiDataAsync<AuthResponse>(response))!.AccessToken);
    }

    [TestCase("GET", "/api/v1/provider/store")]
    [TestCase("PUT", "/api/v1/provider/store")]
    [TestCase("POST", "/api/v1/provider/profile/image")]
    [TestCase("DELETE", "/api/v1/provider/profile/image")]
    public async Task CatalogReadOnlyStaff_CannotAccessStoreEndpoints(string method, string path)
    {
        using var owner = await OwnerClientAsync();
        var before = await ReadApiDataAsync<ProviderStoreDto>(await owner.GetAsync("/api/v1/provider/store"));
        using var staff = await StaffClientAsync(Permissions.ProviderCategory.Read, Permissions.ProviderProduct.Read);
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "PUT")
            request.Content = JsonContent.Create(new { companyName = "Unauthorized store change", phoneNumber = "+201000000099" });
        if (method == "POST")
        {
            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent([1, 2, 3]), "file", "probe.png");
            request.Content = content;
        }

        var response = await staff.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var after = await ReadApiDataAsync<ProviderStoreDto>(await owner.GetAsync("/api/v1/provider/store"));
        after.Should().BeEquivalentTo(before);
    }

    [Test]
    public async Task StoreReadStaff_CanViewStoreButCannotChangeIdentityOrImages()
    {
        using var staff = await StaffClientAsync(Permissions.ProviderStore.Read);
        (await staff.GetAsync("/api/v1/provider/store")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await staff.PutAsJsonAsync("/api/v1/provider/store", new { companyName = "Denied", phoneNumber = "+201000000099" }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await staff.PostAsync("/api/v1/provider/profile/image", new MultipartFormDataContent()))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await staff.DeleteAsync("/api/v1/provider/profile/image")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task StoreEditor_CanPersistCompanyAndPhoneChanges()
    {
        using var staff = await StaffClientAsync(Permissions.ProviderStore.Read, Permissions.ProviderStore.Update);
        var original = await ReadApiDataAsync<ProviderStoreDto>(await staff.GetAsync("/api/v1/provider/store"));
        try
        {
            var response = await staff.PutAsJsonAsync("/api/v1/provider/store", new
            {
                companyName = "Authorized store change",
                phoneNumber = "+201000000099"
            });
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var saved = await ReadApiDataAsync<ProviderStoreDto>(await staff.GetAsync("/api/v1/provider/store"));
            saved!.CompanyName.Should().Be("Authorized store change");
            saved.PhoneNumber.Should().Be("+201000000099");
        }
        finally
        {
            (await staff.PutAsJsonAsync("/api/v1/provider/store", new
            {
                companyName = original!.CompanyName,
                phoneNumber = original.PhoneNumber
            })).EnsureSuccessStatusCode();
        }
    }

    [Test]
    public async Task Owner_HasStorePermissionsAvailableForRoleAssignment()
    {
        using var owner = await OwnerClientAsync();
        (await owner.GetAsync("/api/v1/provider/store")).StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await owner.GetAsync("/api/v1/provider/permissions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var groups = await ReadApiDataAsync<List<PermissionGroupDto>>(response);
        groups!.Single(g => g.Module == "ProviderStore").Permissions.Select(p => p.Name)
            .Should().BeEquivalentTo([Permissions.ProviderStore.Read, Permissions.ProviderStore.Update]);
    }
}
