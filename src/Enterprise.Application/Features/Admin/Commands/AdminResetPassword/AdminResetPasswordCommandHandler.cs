using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Admin.Commands.AdminResetPassword;

public sealed class AdminResetPasswordCommandHandler(
    IUserAccountService userAccountService,
    IOtpService otpService,
    ILogger<AdminResetPasswordCommandHandler> logger) : IRequestHandler<AdminResetPasswordCommand>
{
    public async Task Handle(AdminResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var valid = await otpService.VerifyAndConsumeAsync(
            request.Email, OtpPurpose.ResetPassword, request.Otp, cancellationToken);
        if (!valid)
        {
            logger.LogWarning("Admin reset password failed for {Email}: invalid or expired code", request.Email);
            throw new AuthenticationFailedException(MessageKeys.Auth.InvalidOrExpiredResetCode);
        }

        var user = await userAccountService.FindByEmailAsync(request.Email, cancellationToken);
        if (user is null || user.UserType != UserType.Admin)
        {
            logger.LogWarning("Admin reset password failed for {Email}: user not found or not admin", request.Email);
            throw new AuthenticationFailedException();
        }

        var result = await userAccountService.ResetPasswordAsync(user.Id, request.NewPassword, cancellationToken);
        if (!result.Succeeded)
        {
            throw new ConflictException(result.Error ?? MessageKeys.Auth.UnableToResetPassword);
        }

        logger.LogInformation("Admin password reset succeeded for {Email}", request.Email);
    }
}
