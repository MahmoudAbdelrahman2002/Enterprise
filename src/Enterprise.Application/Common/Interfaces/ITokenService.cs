using Enterprise.Application.Common.Models;

namespace Enterprise.Application.Common.Interfaces;

public sealed record AccessTokenResult(string Token, DateTime ExpiresAtUtc);

/// <summary>
/// Issues JWT access tokens and opaque refresh tokens.
/// </summary>
public interface ITokenService
{
    AccessTokenResult GenerateAccessToken(AuthUserSnapshot user);

    string GenerateRefreshToken();

    string HashToken(string rawToken);
}
