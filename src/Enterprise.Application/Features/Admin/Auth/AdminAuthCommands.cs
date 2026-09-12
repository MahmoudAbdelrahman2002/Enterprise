using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using Enterprise.Application.Features.Auth;
using Enterprise.Application.Features.Auth.Common;
using Enterprise.Domain.Enums;
using FluentValidation;
using MediatR;

namespace Enterprise.Application.Features.Admin.Auth;

public sealed record AdminLoginCommand(string Email, string Password, string? IpAddress) : IRequest<AuthResponseDto>;

public sealed class AdminLoginCommandValidator : AbstractValidator<AdminLoginCommand>
{
    public AdminLoginCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Email).RequiredEmail(localizer);
        RuleFor(x => x.Password).Required(localizer);
    }
}

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

public sealed record AdminForgotPasswordCommand(string Email) : IRequest;

public sealed class AdminForgotPasswordCommandValidator : AbstractValidator<AdminForgotPasswordCommand>
{
    public AdminForgotPasswordCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Email).RequiredEmail(localizer);
    }
}

public sealed class AdminForgotPasswordCommandHandler(
    IUserAccountService userAccountService,
    IOtpService otpService,
    IEmailSender emailSender,
    IAppLocalizer localizer) : IRequestHandler<AdminForgotPasswordCommand>
{
    public async Task Handle(AdminForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userAccountService.FindByEmailAsync(request.Email, cancellationToken);
        if (user is null || user.UserType != UserType.Admin || !user.IsActive)
        {
            return;
        }

        var code = await otpService.IssueAsync(request.Email, OtpPurpose.ResetPassword, cancellationToken);
        try
        {
            await emailSender.SendAsync(
                request.Email,
                localizer[MessageKeys.Email.ResetSubject],
                localizer[MessageKeys.Email.ResetBody, code],
                cancellationToken);
        }
        catch (Exception)
        {
            try
            {
                await otpService.InvalidateAsync(request.Email, OtpPurpose.ResetPassword, cancellationToken);
            }
            catch (Exception)
            {
                // Best-effort cleanup.
            }

            throw;
        }
    }
}

public sealed record AdminResetPasswordCommand(string Email, string Otp, string NewPassword) : IRequest;

public sealed class AdminResetPasswordCommandValidator : AbstractValidator<AdminResetPasswordCommand>
{
    public AdminResetPasswordCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Email).RequiredEmail(localizer);
        RuleFor(x => x.Otp).OtpCode(localizer);
        RuleFor(x => x.NewPassword).StrongPassword(localizer);
    }
}

public sealed class AdminResetPasswordCommandHandler(
    IUserAccountService userAccountService,
    IOtpService otpService) : IRequestHandler<AdminResetPasswordCommand>
{
    public async Task Handle(AdminResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var valid = await otpService.VerifyAndConsumeAsync(
            request.Email, OtpPurpose.ResetPassword, request.Otp, cancellationToken);
        if (!valid)
        {
            throw new AuthenticationFailedException(MessageKeys.Auth.InvalidOrExpiredResetCode);
        }

        var user = await userAccountService.FindByEmailAsync(request.Email, cancellationToken);
        if (user is null || user.UserType != UserType.Admin)
        {
            throw new AuthenticationFailedException();
        }

        var result = await userAccountService.ResetPasswordAsync(user.Id, request.NewPassword, cancellationToken);
        if (!result.Succeeded)
        {
            throw new ConflictException(result.Error ?? MessageKeys.Auth.UnableToResetPassword);
        }
    }
}

public sealed record AdminChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest;

public sealed class AdminChangePasswordCommandValidator : AbstractValidator<AdminChangePasswordCommand>
{
    public AdminChangePasswordCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.CurrentPassword).Required(localizer);
        RuleFor(x => x.NewPassword).StrongPassword(localizer);
    }
}

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
