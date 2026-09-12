namespace Enterprise.Application.Common.Auth;

public sealed record ExternalUserInfo(
    string Provider,
    string ProviderKey,
    string Email,
    string FirstName,
    string LastName,
    bool EmailVerified);
