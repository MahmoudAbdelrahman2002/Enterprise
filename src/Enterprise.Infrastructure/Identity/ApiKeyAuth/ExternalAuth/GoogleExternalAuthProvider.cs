using Enterprise.Application.Common.Auth;
using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Settings;
using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterprise.Infrastructure.Identity.ApiKeyAuth.ExternalAuth;

public sealed class GoogleExternalAuthProvider(
    IOptions<ExternalAuthSettings> options,
    ILogger<GoogleExternalAuthProvider> logger) : IExternalAuthProvider
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
            logger.LogWarning("Google external auth validation failed: provider not configured");
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
                logger.LogWarning("Google external auth validation failed: email missing");
                throw new AuthenticationFailedException(
                    MessageKeys.Auth.SocialEmailRequired);
            }

            if (!payload.EmailVerified)
            {
                logger.LogWarning(
                    "Google external auth validation failed: email not verified for subject {Subject}",
                    payload.Subject);
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
        catch (InvalidJwtException ex)
        {
            logger.LogWarning(ex, "Google external auth validation failed: invalid JWT");
            throw new AuthenticationFailedException(MessageKeys.Auth.InvalidGoogleToken);
        }
    }
}
