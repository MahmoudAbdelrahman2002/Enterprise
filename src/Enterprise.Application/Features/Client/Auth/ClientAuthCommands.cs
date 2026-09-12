using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Settings;
using Enterprise.Application.Common.Validation;
using Enterprise.Application.Features.Auth;
using Enterprise.Application.Features.Auth.Common;
using Enterprise.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace Enterprise.Application.Features.Client.Auth;

/// <summary>
/// Returned after OTP is issued. <see cref="DevelopmentOtp"/> is set only when SMTP is not
/// configured (local/dev), so Swagger users can complete verify without reading server logs.
/// </summary>
public sealed record OtpSentDto(string Message, string? DevelopmentOtp = null);

public sealed record ClientRegisterCommand(string Email, string FirstName, string LastName)
    : IRequest<OtpSentDto>;

public sealed class ClientRegisterCommandValidator : AbstractValidator<ClientRegisterCommand>
{
    public ClientRegisterCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Email).RequiredEmail(localizer).MaxLen(localizer, 256);
        RuleFor(x => x.FirstName).Required(localizer).MaxLen(localizer, 100);
        RuleFor(x => x.LastName).Required(localizer).MaxLen(localizer, 100);
    }
}

public sealed class ClientRegisterCommandHandler(
    IUserAccountService userAccountService,
    IOtpService otpService,
    IEmailSender emailSender,
    IAppLocalizer localizer,
    IOptions<SmtpSettings> smtpOptions,
    IOptions<OtpSettings> otpOptions) : IRequestHandler<ClientRegisterCommand, OtpSentDto>
{
    public async Task<OtpSentDto> Handle(ClientRegisterCommand request, CancellationToken cancellationToken)
    {
        var existing = await userAccountService.FindByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException(MessageKeys.Account.EmailExists);
        }

        var create = await userAccountService.CreateClientAsync(
            request.Email, request.FirstName, request.LastName, cancellationToken);
        if (!create.Succeeded)
        {
            throw new ConflictException(create.Error ?? MessageKeys.Account.UnableToCreate);
        }

        try
        {
            var code = await otpService.IssueAsync(request.Email, OtpPurpose.Register, cancellationToken);
            await emailSender.SendAsync(
                request.Email,
                localizer[MessageKeys.Email.VerificationSubject],
                localizer[MessageKeys.Email.VerificationBody, code],
                cancellationToken);

            return BuildOtpResponse(localizer[MessageKeys.Auth.RegistrationStarted], code);
        }
        catch (OperationCanceledException)
        {
            await CompensateFailedRegistrationAsync(request.Email, cancellationToken);
            throw;
        }
        catch (Exception)
        {
            await CompensateFailedRegistrationAsync(request.Email, cancellationToken);
            throw;
        }
    }

    private async Task CompensateFailedRegistrationAsync(string email, CancellationToken cancellationToken)
    {
        try
        {
            await otpService.InvalidateAsync(email, OtpPurpose.Register, cancellationToken);
        }
        catch (Exception)
        {
            // Best-effort; still attempt user delete.
        }

        try
        {
            await userAccountService.DeleteByEmailAsync(email, cancellationToken);
        }
        catch (Exception)
        {
            // Best-effort rollback; original failure is rethrown by the caller.
        }
    }

    private OtpSentDto BuildOtpResponse(string message, string code) =>
        ShouldExposeOtp()
            ? new OtpSentDto(message, code)
            : new OtpSentDto(message);

    private bool ShouldExposeOtp() =>
        otpOptions.Value.ExposeCodeInResponse || !smtpOptions.Value.IsConfigured;
}

public sealed record ClientVerifyRegistrationCommand(string Email, string Otp, string? IpAddress)
    : IRequest<AuthResponseDto>;

public sealed class ClientVerifyRegistrationCommandValidator : AbstractValidator<ClientVerifyRegistrationCommand>
{
    public ClientVerifyRegistrationCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Email).RequiredEmail(localizer);
        RuleFor(x => x.Otp).OtpCode(localizer);
    }
}

public sealed class ClientVerifyRegistrationCommandHandler(
    IUserAccountService userAccountService,
    IOtpService otpService,
    ITokenIssuanceService tokenIssuanceService) : IRequestHandler<ClientVerifyRegistrationCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(ClientVerifyRegistrationCommand request, CancellationToken cancellationToken)
    {
        var valid = await otpService.VerifyAndConsumeAsync(
            request.Email, OtpPurpose.Register, request.Otp, cancellationToken);
        if (!valid)
        {
            throw new AuthenticationFailedException(MessageKeys.Auth.InvalidOrExpiredVerificationCode);
        }

        var user = await userAccountService.FindByEmailAsync(request.Email, cancellationToken)
            ?? throw new AuthenticationFailedException();

        if (user.UserType != UserType.Client)
        {
            throw new AuthenticationFailedException();
        }

        await userAccountService.ConfirmEmailAsync(user.Id, cancellationToken);
        user = (await userAccountService.FindByIdAsync(user.Id, cancellationToken))!;

        return await tokenIssuanceService.IssueTokensAsync(user, request.IpAddress, cancellationToken);
    }
}

public sealed record ClientLoginCommand(string Email) : IRequest<OtpSentDto>;

public sealed class ClientLoginCommandValidator : AbstractValidator<ClientLoginCommand>
{
    public ClientLoginCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Email).RequiredEmail(localizer);
    }
}

public sealed class ClientLoginCommandHandler(
    IUserAccountService userAccountService,
    IOtpService otpService,
    IEmailSender emailSender,
    IAppLocalizer localizer,
    IOptions<SmtpSettings> smtpOptions,
    IOptions<OtpSettings> otpOptions) : IRequestHandler<ClientLoginCommand, OtpSentDto>
{
    public async Task<OtpSentDto> Handle(ClientLoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userAccountService.FindByEmailAsync(request.Email, cancellationToken);

        if (user is null || user.UserType != UserType.Client || !user.IsActive)
        {
            return new OtpSentDto(localizer[MessageKeys.Auth.OtpSentIfExists]);
        }

        if (!user.EmailConfirmed)
        {
            return await IssueAndSendOtpAsync(
                request.Email,
                OtpPurpose.Register,
                localizer[MessageKeys.Email.VerificationSubject],
                MessageKeys.Email.VerificationBody,
                localizer[MessageKeys.Auth.AccountNotVerified],
                cancellationToken);
        }

        return await IssueAndSendOtpAsync(
            request.Email,
            OtpPurpose.Login,
            localizer[MessageKeys.Email.SignInSubject],
            MessageKeys.Email.SignInBody,
            localizer[MessageKeys.Auth.OtpSentIfExists],
            cancellationToken);
    }

    private async Task<OtpSentDto> IssueAndSendOtpAsync(
        string email,
        OtpPurpose purpose,
        string subject,
        string bodyKey,
        string successMessage,
        CancellationToken cancellationToken)
    {
        var code = await otpService.IssueAsync(email, purpose, cancellationToken);
        try
        {
            await emailSender.SendAsync(
                email,
                subject,
                localizer[bodyKey, code],
                cancellationToken);
        }
        catch (Exception)
        {
            try
            {
                await otpService.InvalidateAsync(email, purpose, cancellationToken);
            }
            catch (Exception)
            {
                // Best-effort cleanup.
            }

            throw;
        }

        return BuildOtpResponse(successMessage, code);
    }

    private OtpSentDto BuildOtpResponse(string message, string code) =>
        ShouldExposeOtp()
            ? new OtpSentDto(message, code)
            : new OtpSentDto(message);

    private bool ShouldExposeOtp() =>
        otpOptions.Value.ExposeCodeInResponse || !smtpOptions.Value.IsConfigured;
}

public sealed record ClientVerifyLoginCommand(string Email, string Otp, string? IpAddress)
    : IRequest<AuthResponseDto>;

public sealed class ClientVerifyLoginCommandValidator : AbstractValidator<ClientVerifyLoginCommand>
{
    public ClientVerifyLoginCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Email).RequiredEmail(localizer);
        RuleFor(x => x.Otp).OtpCode(localizer);
    }
}

public sealed class ClientVerifyLoginCommandHandler(
    IUserAccountService userAccountService,
    IOtpService otpService,
    ITokenIssuanceService tokenIssuanceService) : IRequestHandler<ClientVerifyLoginCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(ClientVerifyLoginCommand request, CancellationToken cancellationToken)
    {
        var valid = await otpService.VerifyAndConsumeAsync(
            request.Email, OtpPurpose.Login, request.Otp, cancellationToken);
        if (!valid)
        {
            throw new AuthenticationFailedException(MessageKeys.Auth.InvalidOrExpiredLoginCode);
        }

        var user = await userAccountService.FindByEmailAsync(request.Email, cancellationToken);
        if (user is null || user.UserType != UserType.Client || !user.IsActive || !user.EmailConfirmed)
        {
            throw new AuthenticationFailedException();
        }

        return await tokenIssuanceService.IssueTokensAsync(user, request.IpAddress, cancellationToken);
    }
}
