using System.Net.Http.Json;
using System.Text.Json;

namespace Enterprise.IntegrationTests;

[TestFixture]
public sealed class TeamDiscoveryTests : TestFixtureBase
{
    [Test]
    public async Task AdminRoleSearch_FiltersSystemRoles_SortsNamesAndPaginates()
    {
        using var admin = AuthenticatedClient((await LoginAsAdminAsync()).AccessToken);
        var prefix = "Discovery" + Guid.NewGuid().ToString("N")[..8];
        foreach (var suffix in new[] { " Alpha", " Charlie", " Bravo" })
            (await admin.PostAsJsonAsync("/api/v1/admin/roles", new { name = new { en = prefix + suffix }, permissions = new[] { "Providers.Read" } })).EnsureSuccessStatusCode();
        var route = $"/api/v1/admin/roles?searchTerm={prefix}&isSystem=false&pageSize=2&pageNumber=1&descending=false";
        var firstResponse = await admin.GetAsync(route); firstResponse.EnsureSuccessStatusCode();
        var first = await ReadApiDataAsync<JsonElement>(firstResponse);
        Assert.That(first.GetProperty("totalCount").GetInt32(), Is.EqualTo(3));
        var names = first.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("name").GetString()).ToArray();
        Assert.That(names, Is.EqualTo(new[] { prefix + " Alpha", prefix + " Bravo" }));
        var second = await ReadApiDataAsync<JsonElement>(await admin.GetAsync(route.Replace("pageNumber=1", "pageNumber=2")));
        Assert.That(second.GetProperty("items").EnumerateArray().Single().GetProperty("name").GetString(), Is.EqualTo(prefix + " Charlie"));
        var descending = await ReadApiDataAsync<JsonElement>(await admin.GetAsync(route.Replace("descending=false", "descending=true")));
        Assert.That(descending.GetProperty("items")[0].GetProperty("name").GetString(), Is.EqualTo(prefix + " Charlie"));
        var system = await ReadApiDataAsync<JsonElement>(await admin.GetAsync(route.Replace("isSystem=false", "isSystem=true")));
        Assert.That(system.GetProperty("totalCount").GetInt32(), Is.Zero);
    }
}
