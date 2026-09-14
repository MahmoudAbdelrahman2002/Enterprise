using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Auth;
using Enterprise.Application.Features.Auth.Common;
using Enterprise.Domain.Enums;
using MediatR;

namespace Enterprise.Application.Features.Admin.Commands.AdminLogin;

public sealed class AdminLoginCommandHandler(
    IUserAccountService userAccountService,
    ITokenIssuanceService tokenIssuanceService) : IRequestHandler<AdminLoginCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(AdminLoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userAccountService.FindByEmailAsync(request.Email, cancellationToken);
        if (user is null || user.UserType != UserType.Admin || !user.IsActive)
        {
            throw new AuthenticationFailedException();
        }

        if (await userAccountService.IsLockedOutAsync(user.Id, cancellationToken))
        {
            throw new AuthenticationFailedException(MessageKeys.Auth.AccountLocked);
        }

        if (!await userAccountService.CheckPasswordAsync(user.Id, request.Password, cancellationToken))
        {
            await userAccountService.AccessFailedAsync(user.Id, cancellationToken);
            throw new AuthenticationFailedException();
        }

        await userAccountService.ResetAccessFailedAsync(user.Id, cancellationToken);
        return await tokenIssuanceService.IssueTokensAsync(user, request.IpAddress, cancellationToken);
    }
}
