using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NUnit.Framework;

namespace Enterprise.IntegrationTests;

[TestFixture]
public class AuthEndpointsTests : TestFixtureBase
{
    [Test]
    public async Task AdminLogin_ValidCredentials_ReturnsTokensWithAdminUserType()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/admin/auth/login", new
        {
            email = "admin@enterprise.local",
            password = "Admin@12345!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await ReadApiResponseAsync<AuthResponse>(response);
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
        envelope.Data.User.UserType.Should().Be("Admin");
        envelope.Data.User.Roles.Should().Contain("Admin");
        envelope.TraceId.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task AdminLogin_WrongPassword_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/admin/auth/login", new
        {
            email = "admin@enterprise.local",
            password = "WrongPassword1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var envelope = await ReadApiResponseAsync<object>(response);
        envelope!.Success.Should().BeFalse();
        envelope.Errors.Should().NotBeEmpty();
    }

    [Test]
    public async Task ClientRegister_ThenVerify_ReturnsTokens()
    {
        var email = UniqueEmail();

        var register = await Client.PostAsJsonAsync("/api/v1/client/auth/register", new
        {
            email,
            firstName = "Ada",
            lastName = "Lovelace"
        });
        register.StatusCode.Should().Be(HttpStatusCode.OK);

        CapturingEmailSender.CapturedOtps.Should().ContainKey(email.ToLowerInvariant());
        var otp = CapturingEmailSender.CapturedOtps[email.ToLowerInvariant()];

        var verify = await Client.PostAsJsonAsync("/api/v1/client/auth/verify-registration", new
        {
            email,
            otp
        });

        verify.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadApiDataAsync<AuthResponse>(verify);
        body!.User.Email.Should().Be(email);
        body.User.UserType.Should().Be("Client");
        body.User.Roles.Should().Contain("Client");
    }

    [Test]
    public async Task ClientLogin_ThenVerify_ReturnsTokens()
    {
        var email = UniqueEmail();
        await RegisterClientAsync(email);

        var login = await Client.PostAsJsonAsync("/api/v1/client/auth/login", new { email });
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var otp = CapturingEmailSender.CapturedOtps[email.ToLowerInvariant()];
        var verify = await Client.PostAsJsonAsync("/api/v1/client/auth/verify-login", new { email, otp });
        verify.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task AdminProfile_Get_ReturnsProfile()
    {
        var admin = await LoginAsAdminAsync();
        using var client = AuthenticatedClient(admin.AccessToken);

        var response = await client.GetAsync("/api/v1/admin/profile");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await ReadApiDataAsync<ProfileResponse>(response);
        profile!.Email.Should().Be("admin@enterprise.local");
        profile.UserType.Should().Be("Admin");
    }

    [Test]
    public async Task ClientCannotAccessAdminProfile()
    {
        var registration = await RegisterClientAsync();
        using var client = AuthenticatedClient(registration.AccessToken);

        var response = await client.GetAsync("/api/v1/admin/profile");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task AdminRefreshToken_ReturnsNewPair()
    {
        var admin = await LoginAsAdminAsync();
        var response = await Client.PostAsJsonAsync("/api/v1/admin/auth/refresh-token", new
        {
            refreshToken = admin.RefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadApiDataAsync<AuthResponse>(response);
        body!.RefreshToken.Should().NotBe(admin.RefreshToken);
    }
}

public sealed record ProfileResponse(
    Guid Id, string Email, string FirstName, string LastName, string UserType, bool EmailConfirmed);
