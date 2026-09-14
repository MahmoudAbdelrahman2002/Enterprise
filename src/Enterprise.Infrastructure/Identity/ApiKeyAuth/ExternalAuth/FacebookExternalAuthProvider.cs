using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Enterprise.Application.Common.Auth;
using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Settings;
using Microsoft.Extensions.Options;

namespace Enterprise.Infrastructure.Identity.ApiKeyAuth.ExternalAuth;

public sealed class FacebookExternalAuthProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<ExternalAuthSettings> options) : IExternalAuthProvider
{
    public string ProviderName => ExternalAuthProviders.Facebook;

    public async Task<ExternalUserInfo> ValidateAsync(
        string idToken, CancellationToken cancellationToken = default)
    {
        var settings = options.Value.Facebook;
        if (string.IsNullOrWhiteSpace(settings.AppId)
            || string.IsNullOrWhiteSpace(settings.AppSecret)
            || settings.AppId.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase)
            || settings.AppSecret.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
        {
            throw new AuthenticationFailedException(MessageKeys.Auth.FacebookNotConfigured);
        }

        var client = httpClientFactory.CreateClient(nameof(FacebookExternalAuthProvider));
        var appAccessToken = $"{settings.AppId}|{settings.AppSecret}";

        var debugUrl =
            $"debug_token?input_token={Uri.EscapeDataString(idToken)}&access_token={Uri.EscapeDataString(appAccessToken)}";
        using var debugResponse = await client.GetAsync(debugUrl, cancellationToken);
        if (!debugResponse.IsSuccessStatusCode)
        {
            throw new AuthenticationFailedException(MessageKeys.Auth.InvalidFacebookToken);
        }

        var debug = await debugResponse.Content.ReadFromJsonAsync<FacebookDebugTokenResponse>(
            cancellationToken: cancellationToken);
        var data = debug?.Data;
        if (data is null
            || !data.IsValid
            || !string.Equals(data.AppId, settings.AppId, StringComparison.Ordinal))
        {
            throw new AuthenticationFailedException(MessageKeys.Auth.InvalidFacebookToken);
        }

        var meUrl =
            $"me?fields=id,email,first_name,last_name,name&access_token={Uri.EscapeDataString(idToken)}";
        using var meResponse = await client.GetAsync(meUrl, cancellationToken);
        if (!meResponse.IsSuccessStatusCode)
        {
            throw new AuthenticationFailedException(MessageKeys.Auth.FacebookProfileFailed);
        }

        var me = await meResponse.Content.ReadFromJsonAsync<FacebookMeResponse>(
            cancellationToken: cancellationToken);
        if (me is null || string.IsNullOrWhiteSpace(me.Id))
        {
            throw new AuthenticationFailedException(MessageKeys.Auth.FacebookProfileFailed);
        }

        if (string.IsNullOrWhiteSpace(me.Email))
        {
            throw new AuthenticationFailedException(
                MessageKeys.Auth.SocialEmailRequired);
        }

        var first = me.FirstName;
        var last = me.LastName;
        if (string.IsNullOrWhiteSpace(first) && !string.IsNullOrWhiteSpace(me.Name))
        {
            var parts = me.Name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            first = parts.ElementAtOrDefault(0) ?? "User";
            last = parts.ElementAtOrDefault(1) ?? "Client";
        }

        return new ExternalUserInfo(
            ExternalAuthProviders.Facebook,
            me.Id,
            me.Email,
            string.IsNullOrWhiteSpace(first) ? "User" : first,
            string.IsNullOrWhiteSpace(last) ? "Client" : last,
            // Facebook only returns email when the user granted email permission.
            EmailVerified: true);
    }

    private sealed class FacebookDebugTokenResponse
    {
        [JsonPropertyName("data")]
        public FacebookDebugTokenData? Data { get; init; }
    }

    private sealed class FacebookDebugTokenData
    {
        [JsonPropertyName("app_id")]
        public string? AppId { get; init; }

        [JsonPropertyName("is_valid")]
        public bool IsValid { get; init; }

        [JsonPropertyName("user_id")]
        public string? UserId { get; init; }
    }

    private sealed class FacebookMeResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("email")]
        public string? Email { get; init; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; init; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }
}
