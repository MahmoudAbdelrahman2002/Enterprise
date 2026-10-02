using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Notifications.Commands.RegisterDeviceToken;

public sealed class RegisterDeviceTokenCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<RegisterDeviceTokenCommand>
{
    public async Task Handle(RegisterDeviceTokenCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new AuthenticationFailedException(MessageKeys.Error.Unauthorized);

        var token = request.Token.Trim();
        var existing = await unitOfWork.DeviceTokens.GetByTokenAsync(token, cancellationToken);
        if (existing is null)
        {
            unitOfWork.DeviceTokens.Add(new DeviceToken(userId, token, NormalizePlatform(request.Platform)));
        }
        else
        {
            existing.Update(userId, NormalizePlatform(request.Platform));
            unitOfWork.DeviceTokens.Update(existing);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string? NormalizePlatform(string? platform) =>
        string.IsNullOrWhiteSpace(platform) ? null : platform.Trim().ToLowerInvariant();
}
