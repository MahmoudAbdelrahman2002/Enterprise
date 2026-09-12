using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NUnit.Framework;

namespace Enterprise.IntegrationTests;

[TestFixture]
public abstract class TestFixtureBase
{
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected CustomWebApplicationFactory Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;

    [OneTimeSetUp]
    public void BaseOneTimeSetUp()
    {
        Factory = new CustomWebApplicationFactory();
        Client = Factory.CreateClient();
        CapturingEmailSender.CapturedOtps.Clear();
    }

    [OneTimeTearDown]
    public void BaseOneTimeTearDown()
    {
        Client.Dispose();
        Factory.Dispose();
    }

    protected static string UniqueEmail([System.Runtime.CompilerServices.CallerMemberName] string? prefix = null) =>
        $"{prefix?.ToLowerInvariant() ?? "user"}-{Guid.NewGuid():N}@example.com";

    protected static string UniqueSku([System.Runtime.CompilerServices.CallerMemberName] string? prefix = null) =>
        $"SKU-{prefix}-{Guid.NewGuid():N}"[..Math.Min(40, $"SKU-{prefix}-{Guid.NewGuid():N}".Length)];

    protected async Task<AuthResponse> RegisterClientAsync(string? email = null)
    {
        email ??= UniqueEmail();
        var register = await Client.PostAsJsonAsync("/api/v1/client/auth/register", new
        {
            email,
            firstName = "Test",
            lastName = "User"
        });
        register.EnsureSuccessStatusCode();

        if (!CapturingEmailSender.CapturedOtps.TryGetValue(email.ToLowerInvariant(), out var otp))
        {
            throw new InvalidOperationException($"OTP was not captured for {email}");
        }

        var verify = await Client.PostAsJsonAsync("/api/v1/client/auth/verify-registration", new { email, otp });
        verify.EnsureSuccessStatusCode();
        return (await ReadApiDataAsync<AuthResponse>(verify))!;
    }

    /// <summary> Backward-compatible alias used by product tests. </summary>
    protected Task<AuthResponse> RegisterAsync(string? email = null) => RegisterClientAsync(email);

    protected async Task<AuthResponse> LoginAsAdminAsync()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/admin/auth/login", new
        {
            email = "admin@enterprise.local",
            password = "Admin@12345!"
        });

        response.EnsureSuccessStatusCode();
        return (await ReadApiDataAsync<AuthResponse>(response))!;
    }

    protected HttpClient AuthenticatedClient(string accessToken)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    protected static async Task<T?> ReadApiDataAsync<T>(HttpResponseMessage response)
    {
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponseDto<T>>(JsonOptions);
        return envelope is null ? default : envelope.Data;
    }

    protected static async Task<ApiResponseDto<T>?> ReadApiResponseAsync<T>(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<ApiResponseDto<T>>(JsonOptions);
}

public sealed record ApiResponseDto<T>(
    bool Success,
    int StatusCode,
    string Message,
    IReadOnlyList<string> Errors,
    T? Data,
    string? TraceId);

public sealed record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    UserInfo User);

public sealed record UserInfo(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string UserType,
    List<string> Roles);
