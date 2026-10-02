using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Auth.Common;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    IUnitOfWork unitOfWork,
    ITokenService tokenService,
    ITokenIssuanceService tokenIssuanceService,
    IUserAccountService userAccountService,
    ILogger<RefreshTokenCommandHandler> logger) : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = tokenService.HashToken(request.RefreshToken);
        var storedToken = await unitOfWork.RefreshTokens.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (storedToken is null)
        {
            logger.LogWarning("Refresh token failed: token not found");
            throw new AuthenticationFailedException(MessageKeys.Auth.InvalidRefreshToken);
        }

        if (storedToken.IsRevoked)
        {
            logger.LogWarning("Refresh token reuse detected for {UserId}", storedToken.UserId);
            await RevokeAllActiveTokensAsync(storedToken.UserId, request.IpAddress, cancellationToken);
            throw new AuthenticationFailedException(
                MessageKeys.Auth.RefreshTokenReused);
        }

        if (storedToken.IsExpired)
        {
            logger.LogWarning("Refresh token expired for {UserId}", storedToken.UserId);
            throw new AuthenticationFailedException(MessageKeys.Auth.RefreshTokenExpired);
        }

        var user = await userAccountService.FindByIdAsync(storedToken.UserId, cancellationToken);
        if (user is null || !user.IsActive || user.UserType != request.ExpectedUserType)
        {
            logger.LogWarning("Refresh token failed for {UserId}: inactive or wrong user type", storedToken.UserId);
            throw new AuthenticationFailedException();
        }

        var newTokens = await tokenIssuanceService.IssueTokensAsync(user, request.IpAddress, cancellationToken);
        var newRefreshTokenHash = tokenService.HashToken(newTokens.RefreshToken);

        storedToken.Revoke(request.IpAddress, "Rotated on refresh.", newRefreshTokenHash);
        unitOfWork.RefreshTokens.Update(storedToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Refresh token succeeded for {UserId}", user.Id);
        return newTokens;
    }

    private async Task RevokeAllActiveTokensAsync(Guid userId, string? ipAddress, CancellationToken cancellationToken)
    {
        var activeTokens = await unitOfWork.RefreshTokens.GetActiveTokensByUserIdAsync(userId, cancellationToken);
        foreach (var token in activeTokens)
        {
            token.Revoke(ipAddress, "Revoked: possible refresh token reuse detected.");
            unitOfWork.RefreshTokens.Update(token);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
