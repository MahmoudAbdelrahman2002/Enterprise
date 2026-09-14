using Enterprise.Application.Common.Auth;
using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Localization;

namespace Enterprise.Infrastructure.Identity.ApiKeyAuth.ExternalAuth;

public sealed class ExternalAuthProviderResolver(IEnumerable<IExternalAuthProvider> providers)
    : IExternalAuthProviderResolver
{
    private readonly IReadOnlyDictionary<string, IExternalAuthProvider> _providers =
        providers.ToDictionary(p => p.ProviderName, StringComparer.OrdinalIgnoreCase);

    public IExternalAuthProvider Resolve(string provider)
    {
        var key = ExternalAuthProviders.Normalize(provider);
        if (_providers.TryGetValue(key, out var resolved))
        {
            return resolved;
        }

        throw new AuthenticationFailedException(MessageKeys.Auth.UnsupportedProvider);
    }
}
