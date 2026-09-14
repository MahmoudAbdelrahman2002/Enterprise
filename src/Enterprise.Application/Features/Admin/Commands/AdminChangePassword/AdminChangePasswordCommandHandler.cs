using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Enums;
using MediatR;

namespace Enterprise.Application.Features.Admin.Commands.AdminChangePassword;

public sealed class AdminChangePasswordCommandHandler(
    ICurrentUserService currentUserService,
    IUserAccountService userAccountService) : IRequestHandler<AdminChangePasswordCommand>
{
    public async Task Handle(AdminChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new AuthenticationFailedException();

        await userAccountService.EnsureUserTypeAsync(userId, UserType.Admin, cancellationToken);

        var result = await userAccountService.ChangePasswordAsync(
            userId, request.CurrentPassword, request.NewPassword, cancellationToken);
        if (!result.Succeeded)
        {
            throw new AuthenticationFailedException(result.Error ?? MessageKeys.Auth.UnableToChangePassword);
        }
    }
}
