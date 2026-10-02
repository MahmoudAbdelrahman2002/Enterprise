using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Auth;
using Enterprise.Application.Features.Auth.Common;
using Enterprise.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Admin.Commands.AdminLogin;

public sealed class AdminLoginCommandHandler(
    IUserAccountService userAccountService,
    ITokenIssuanceService tokenIssuanceService,
    ILogger<AdminLoginCommandHandler> logger) : IRequestHandler<AdminLoginCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(AdminLoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userAccountService.FindByEmailAsync(request.Email, cancellationToken);
        if (user is null || user.UserType != UserType.Admin || !user.IsActive)
        {
            logger.LogWarning("Admin login failed for {Email}: invalid credentials or inactive", request.Email);
            throw new AuthenticationFailedException();
        }

        if (await userAccountService.IsLockedOutAsync(user.Id, cancellationToken))
        {
            logger.LogWarning("Admin login failed for {Email}: account locked", request.Email);
            throw new AuthenticationFailedException(MessageKeys.Auth.AccountLocked);
        }

        if (!await userAccountService.CheckPasswordAsync(user.Id, request.Password, cancellationToken))
        {
            await userAccountService.AccessFailedAsync(user.Id, cancellationToken);
            logger.LogWarning("Admin login failed for {Email}: bad password", request.Email);
            throw new AuthenticationFailedException();
        }

        await userAccountService.ResetAccessFailedAsync(user.Id, cancellationToken);
        var response = await tokenIssuanceService.IssueTokensAsync(user, request.IpAddress, cancellationToken);
        logger.LogInformation("Admin login succeeded for {Email}", request.Email);
        return response;
    }
}
