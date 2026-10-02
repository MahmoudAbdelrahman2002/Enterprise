using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Auth.Commands.RevokeToken;

public sealed class RevokeTokenCommandHandler(
    IUnitOfWork unitOfWork,
    ITokenService tokenService,
    ILogger<RevokeTokenCommandHandler> logger)
    : IRequestHandler<RevokeTokenCommand>
{
    public async Task Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = tokenService.HashToken(request.RefreshToken);
        var storedToken = await unitOfWork.RefreshTokens.GetByTokenHashAsync(tokenHash, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Auth.RefreshTokenNotFound);

        if (!storedToken.IsActive)
        {
            return; // already revoked/expired - logout is idempotent, not an error.
        }

        storedToken.Revoke(request.IpAddress, "Revoked by user (logout).");
        unitOfWork.RefreshTokens.Update(storedToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Revoked refresh token for {UserId}", storedToken.UserId);
    }
}
