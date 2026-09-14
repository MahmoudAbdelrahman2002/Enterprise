using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Enums;
using MediatR;

namespace Enterprise.Application.Features.Provider.Commands.ProviderChangePassword;

public sealed class ProviderChangePasswordCommandHandler(
    ICurrentUserService currentUserService,
    IUserAccountService userAccountService) : IRequestHandler<ProviderChangePasswordCommand>
{
    public async Task Handle(ProviderChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new AuthenticationFailedException();

        await userAccountService.EnsureUserTypeAsync(userId, UserType.Provider, cancellationToken);

        var result = await userAccountService.ChangePasswordAsync(
            userId, request.CurrentPassword, request.NewPassword, cancellationToken);
        if (!result.Succeeded)
        {
            throw new AuthenticationFailedException(result.Error ?? MessageKeys.Auth.UnableToChangePassword);
        }
    }
}
