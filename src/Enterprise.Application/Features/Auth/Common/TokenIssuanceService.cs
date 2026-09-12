using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Common.Settings;
using Enterprise.Application.Features.Auth;
using Enterprise.Application.Features.Auth.Common;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace Enterprise.Application.Features.Auth.Common;

public sealed class TokenIssuanceService(
    ITokenService tokenService,
    IUnitOfWork unitOfWork,
    IOptions<JwtSettings> jwtSettings) : ITokenIssuanceService
{
    public async Task<AuthResponseDto> IssueTokensAsync(
        AuthUserSnapshot user, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var accessToken = tokenService.GenerateAccessToken(user);
        var rawRefreshToken = tokenService.GenerateRefreshToken();
        var refreshTokenHash = tokenService.HashToken(rawRefreshToken);

        var refreshTokenExpiry = DateTime.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpirationDays);
        var refreshToken = new RefreshToken(user.Id, refreshTokenHash, refreshTokenExpiry, ipAddress);

        unitOfWork.RefreshTokens.Add(refreshToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var userDto = new UserDto(
            user.Id, user.Email, user.FirstName, user.LastName, user.UserType.ToString(), user.Roles);

        return new AuthResponseDto(accessToken.Token, accessToken.ExpiresAtUtc, rawRefreshToken, userDto);
    }
}
