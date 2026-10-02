using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using Enterprise.Application.Features.Auth;
using Enterprise.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Profiles;

public sealed record GetProfileQuery : IRequest<ProfileDto>;

public sealed class GetProfileQueryHandler(
    ICurrentUserService currentUserService,
    IUserAccountService userAccountService,
    ILogger<GetProfileQueryHandler> logger) : IRequestHandler<GetProfileQuery, ProfileDto>
{
    public async Task<ProfileDto> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new AuthenticationFailedException();
        var user = await userAccountService.FindByIdAsync(userId, cancellationToken)
            ?? throw NotFoundException.For("User", userId);

        logger.LogInformation("Fetched profile for user {UserId} with email {Email}", user.Id, user.Email);
        return new ProfileDto(
            user.Id, user.Email, user.FirstName, user.LastName, user.UserType.ToString(), user.EmailConfirmed);
    }
}

public sealed record UpdateProfileCommand(string FirstName, string LastName) : IRequest<ProfileDto>;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.FirstName).Required(localizer).MaxLen(localizer, 100);
        RuleFor(x => x.LastName).Required(localizer).MaxLen(localizer, 100);
    }
}

public sealed class UpdateProfileCommandHandler(
    ICurrentUserService currentUserService,
    IUserAccountService userAccountService,
    ILogger<UpdateProfileCommandHandler> logger) : IRequestHandler<UpdateProfileCommand, ProfileDto>
{
    public async Task<ProfileDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new AuthenticationFailedException();
        await userAccountService.UpdateProfileAsync(userId, request.FirstName, request.LastName, cancellationToken);
        var user = (await userAccountService.FindByIdAsync(userId, cancellationToken))!;
        logger.LogInformation("Updated profile for user {UserId} with email {Email}", user.Id, user.Email);
        return new ProfileDto(
            user.Id, user.Email, user.FirstName, user.LastName, user.UserType.ToString(), user.EmailConfirmed);
    }
}

public sealed record RequestChangeEmailCommand(string NewEmail) : IRequest;

public sealed class RequestChangeEmailCommandValidator : AbstractValidator<RequestChangeEmailCommand>
{
    public RequestChangeEmailCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.NewEmail).RequiredEmail(localizer).MaxLen(localizer, 256);
    }
}

public sealed class RequestChangeEmailCommandHandler(
    ICurrentUserService currentUserService,
    IUserAccountService userAccountService,
    IOtpService otpService,
    IEmailSender emailSender,
    IAppLocalizer localizer,
    ILogger<RequestChangeEmailCommandHandler> logger) : IRequestHandler<RequestChangeEmailCommand>
{
    public async Task Handle(RequestChangeEmailCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new AuthenticationFailedException();
        var user = await userAccountService.FindByIdAsync(userId, cancellationToken)
            ?? throw NotFoundException.For("User", userId);

        var existing = await userAccountService.FindByEmailAsync(request.NewEmail, cancellationToken);
        if (existing is not null && existing.Id != userId)
        {
            throw new ConflictException(MessageKeys.Account.EmailExists);
        }

        var code = await otpService.IssueAsync(request.NewEmail, OtpPurpose.ChangeEmail, cancellationToken);
        try
        {
            await emailSender.SendAsync(
                request.NewEmail,
                localizer[MessageKeys.Email.ChangeEmailSubject],
                localizer[MessageKeys.Email.ChangeEmailBody, code],
                cancellationToken);
        }
        catch (Exception)
        {
            try
            {
                await otpService.InvalidateAsync(request.NewEmail, OtpPurpose.ChangeEmail, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Best-effort OTP invalidate failed after change-email send failure for {NewEmail}",
                    request.NewEmail);
            }

            throw;
        }

        logger.LogInformation(
            "Change-email code sent for user {UserId} to {NewEmail}",
            userId,
            request.NewEmail);
    }
}

public sealed record ConfirmChangeEmailCommand(string NewEmail, string Otp) : IRequest<ProfileDto>;

public sealed class ConfirmChangeEmailCommandValidator : AbstractValidator<ConfirmChangeEmailCommand>
{
    public ConfirmChangeEmailCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.NewEmail).RequiredEmail(localizer);
        RuleFor(x => x.Otp).OtpCode(localizer);
    }
}

public sealed class ConfirmChangeEmailCommandHandler(
    ICurrentUserService currentUserService,
    IUserAccountService userAccountService,
    IOtpService otpService,
    ILogger<ConfirmChangeEmailCommandHandler> logger) : IRequestHandler<ConfirmChangeEmailCommand, ProfileDto>
{
    public async Task<ProfileDto> Handle(ConfirmChangeEmailCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new AuthenticationFailedException();

        var valid = await otpService.VerifyAndConsumeAsync(
            request.NewEmail, OtpPurpose.ChangeEmail, request.Otp, cancellationToken);
        if (!valid)
        {
            throw new AuthenticationFailedException(MessageKeys.Auth.InvalidOrExpiredVerificationCode);
        }

        var result = await userAccountService.ChangeEmailAsync(userId, request.NewEmail, cancellationToken);
        if (!result.Succeeded)
        {
            throw new ConflictException(result.Error ?? MessageKeys.Account.UnableToChangeEmail);
        }

        var user = (await userAccountService.FindByIdAsync(userId, cancellationToken))!;
        logger.LogInformation("Email changed for user {UserId} to {Email}", user.Id, user.Email);
        return new ProfileDto(
            user.Id, user.Email, user.FirstName, user.LastName, user.UserType.ToString(), user.EmailConfirmed);
    }
}
