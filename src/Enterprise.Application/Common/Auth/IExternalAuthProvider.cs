namespace Enterprise.Application.Common.Auth;

public interface IExternalAuthProvider
{
    string ProviderName { get; }

    Task<ExternalUserInfo> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}
