using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Notifications.Commands.UnregisterDeviceToken;

public sealed class UnregisterDeviceTokenCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<UnregisterDeviceTokenCommand>
{
    public async Task Handle(UnregisterDeviceTokenCommand request, CancellationToken cancellationToken)
    {
        _ = currentUserService.UserId
            ?? throw new AuthenticationFailedException(MessageKeys.Error.Unauthorized);

        await unitOfWork.DeviceTokens.RemoveByTokenAsync(request.Token.Trim(), cancellationToken);
    }
}
