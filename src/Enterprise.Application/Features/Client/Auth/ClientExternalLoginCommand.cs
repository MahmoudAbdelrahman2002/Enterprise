using Enterprise.Application.Common.Auth;
using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Common.Validation;
using Enterprise.Application.Features.Auth;
using Enterprise.Application.Features.Auth.Common;
using Enterprise.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Client.Auth;

public sealed record ClientExternalLoginCommand(string Provider, string IdToken, string? IpAddress)
    : IRequest<AuthResponseDto>;

public sealed class ClientExternalLoginCommandValidator : AbstractValidator<ClientExternalLoginCommand>
{
    public ClientExternalLoginCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Provider)
            .Required(localizer)
            .Must(ExternalAuthProviders.IsSupported)
            .WithMessage(_ => localizer[MessageKeys.Validation.ProviderUnsupported]);
        RuleFor(x => x.IdToken)
            .Required(localizer)
            .MaxLen(localizer, 8192);
    }
}

public sealed class ClientExternalLoginCommandHandler(
    IExternalAuthProviderResolver providerResolver,
    IUserAccountService userAccountService,
    ITokenIssuanceService tokenIssuanceService,
    ILogger<ClientExternalLoginCommandHandler> logger) : IRequestHandler<ClientExternalLoginCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(
        ClientExternalLoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var providerName = ExternalAuthProviders.Normalize(request.Provider);
            var provider = providerResolver.Resolve(providerName);
            var external = await provider.ValidateAsync(request.IdToken, cancellationToken);

            if (string.IsNullOrWhiteSpace(external.Email) || !external.EmailVerified)
            {
                throw new AuthenticationFailedException(
                    MessageKeys.Auth.SocialEmailRequired);
            }

            var byLogin = await userAccountService.FindByLoginAsync(
                external.Provider, external.ProviderKey, cancellationToken);
            if (byLogin is not null)
            {
                EnsureEligibleClient(byLogin);
                var tokens = await tokenIssuanceService.IssueTokensAsync(byLogin, request.IpAddress, cancellationToken);
                logger.LogInformation(
                    "Client external login succeeded via existing login for user {UserId} ({Email}) provider {Provider}",
                    byLogin.Id, byLogin.Email, providerName);
                return tokens;
            }

            var byEmail = await userAccountService.FindByEmailAsync(external.Email, cancellationToken);
            if (byEmail is not null)
            {
                if (byEmail.UserType != UserType.Client)
                {
                    throw new AuthenticationFailedException();
                }

                EnsureEligibleClient(byEmail);

                var link = await userAccountService.AddLoginAsync(
                    byEmail.Id,
                    external.Provider,
                    external.ProviderKey,
                    external.Provider,
                    cancellationToken);
                if (!link.Succeeded)
                {
                    throw new AuthenticationFailedException(link.Error ?? MessageKeys.Auth.SocialLinkFailed);
                }

                if (!byEmail.EmailConfirmed)
                {
                    await userAccountService.ConfirmEmailAsync(byEmail.Id, cancellationToken);
                    byEmail = (await userAccountService.FindByIdAsync(byEmail.Id, cancellationToken))!;
                }

                var tokens = await tokenIssuanceService.IssueTokensAsync(byEmail, request.IpAddress, cancellationToken);
                logger.LogInformation(
                    "Client external login linked to existing user {UserId} ({Email}) provider {Provider}",
                    byEmail.Id, byEmail.Email, providerName);
                return tokens;
            }

            return await CreateExternalClientAsync(external, request.IpAddress, cancellationToken);
        }
        catch (Exception ex) when (
            ex is not AuthenticationFailedException
            and not ConflictException
            and not ForbiddenAccessException
            and not OperationCanceledException)
        {
            throw new AuthenticationFailedException(MessageKeys.Auth.SocialSignInFailed, ex);
        }
    }

    private async Task<AuthResponseDto> CreateExternalClientAsync(
        ExternalUserInfo external, string? ipAddress, CancellationToken cancellationToken)
    {
        var create = await userAccountService.CreateClientFromExternalAsync(
            external.Email,
            string.IsNullOrWhiteSpace(external.FirstName) ? "User" : external.FirstName,
            string.IsNullOrWhiteSpace(external.LastName) ? "Client" : external.LastName,
            cancellationToken);
        if (!create.Succeeded)
        {
            throw new ConflictException(create.Error ?? MessageKeys.Account.UnableToCreate);
        }

        try
        {
            var created = await userAccountService.FindByEmailAsync(external.Email, cancellationToken)
                ?? throw new AuthenticationFailedException();

            var addLogin = await userAccountService.AddLoginAsync(
                created.Id,
                external.Provider,
                external.ProviderKey,
                external.Provider,
                cancellationToken);
            if (!addLogin.Succeeded)
            {
                throw new AuthenticationFailedException(addLogin.Error ?? MessageKeys.Auth.SocialLinkFailed);
            }

            var tokens = await tokenIssuanceService.IssueTokensAsync(created, ipAddress, cancellationToken);
            logger.LogInformation(
                "Client external login created user {UserId} ({Email}) provider {Provider}",
                created.Id, created.Email, external.Provider);
            return tokens;
        }
        catch (Exception)
        {
            try
            {
                await userAccountService.DeleteByEmailAsync(external.Email, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Best-effort user delete failed during external login rollback for {Email} provider {Provider}",
                    external.Email, external.Provider);
            }

            throw;
        }
    }

    private static void EnsureEligibleClient(AuthUserSnapshot user)
    {
        if (user.UserType != UserType.Client || !user.IsActive)
        {
            throw new AuthenticationFailedException();
        }
    }
}
