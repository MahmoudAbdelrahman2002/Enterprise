using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Auth;
using Enterprise.Application.Features.Auth.Common;
using Enterprise.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Commands.ProviderLogin;

public sealed class ProviderLoginCommandHandler(
    IUserAccountService userAccountService,
    ITokenIssuanceService tokenIssuanceService,
    ILogger<ProviderLoginCommandHandler> logger) : IRequestHandler<ProviderLoginCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(ProviderLoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userAccountService.FindByEmailAsync(request.Email, cancellationToken);
        if (user is null || user.UserType != UserType.Provider)
        {
            logger.LogWarning("Provider login failed for {Email}: invalid credentials", request.Email);
            throw new AuthenticationFailedException();
        }

        if (await userAccountService.IsLockedOutAsync(user.Id, cancellationToken))
        {
            logger.LogWarning("Provider login failed for {Email}: account locked", request.Email);
            throw new AuthenticationFailedException(MessageKeys.Auth.AccountLocked);
        }

        if (!await userAccountService.CheckPasswordAsync(user.Id, request.Password, cancellationToken))
        {
            await userAccountService.AccessFailedAsync(user.Id, cancellationToken);
            logger.LogWarning("Provider login failed for {Email}: bad password", request.Email);
            throw new AuthenticationFailedException();
        }

        if (!user.IsActive)
        {
            logger.LogWarning("Provider login rejected for {Email}: account deactivated", request.Email);
            throw new AuthenticationFailedException(MessageKeys.Auth.ProviderDeactivated);
        }

        await userAccountService.ResetAccessFailedAsync(user.Id, cancellationToken);
        var response = await tokenIssuanceService.IssueTokensAsync(user, request.IpAddress, cancellationToken);
        logger.LogInformation("Provider login succeeded for {Email}", request.Email);
        return response;
    }
}
