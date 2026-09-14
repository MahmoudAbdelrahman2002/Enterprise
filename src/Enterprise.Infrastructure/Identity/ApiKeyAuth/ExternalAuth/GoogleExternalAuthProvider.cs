using Enterprise.Application.Common.Auth;
using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Settings;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace Enterprise.Infrastructure.Identity.ApiKeyAuth.ExternalAuth;

public sealed class GoogleExternalAuthProvider(IOptions<ExternalAuthSettings> options) : IExternalAuthProvider
{
    public string ProviderName => ExternalAuthProviders.Google;

    public async Task<ExternalUserInfo> ValidateAsync(
        string idToken, CancellationToken cancellationToken = default)
    {
        var clientIds = options.Value.Google.ClientIds
            .Where(id => !string.IsNullOrWhiteSpace(id)
                         && !id.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (clientIds.Length == 0)
        {
            throw new AuthenticationFailedException(MessageKeys.Auth.GoogleNotConfigured);
        }

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = clientIds
                });

            if (string.IsNullOrWhiteSpace(payload.Email))
            {
                throw new AuthenticationFailedException(
                    MessageKeys.Auth.SocialEmailRequired);
            }

            if (!payload.EmailVerified)
            {
                throw new AuthenticationFailedException(
                    MessageKeys.Auth.SocialEmailRequired);
            }

            var given = payload.GivenName ?? string.Empty;
            var family = payload.FamilyName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(given) && !string.IsNullOrWhiteSpace(payload.Name))
            {
                var parts = payload.Name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                given = parts.ElementAtOrDefault(0) ?? "User";
                family = parts.ElementAtOrDefault(1) ?? "Client";
            }

            return new ExternalUserInfo(
                ExternalAuthProviders.Google,
                payload.Subject,
                payload.Email,
                string.IsNullOrWhiteSpace(given) ? "User" : given,
                string.IsNullOrWhiteSpace(family) ? "Client" : family,
                EmailVerified: true);
        }
        catch (InvalidJwtException)
        {
            throw new AuthenticationFailedException(MessageKeys.Auth.InvalidGoogleToken);
        }
    }
}
