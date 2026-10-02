using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Admin.Commands.AdminForgotPassword;

public sealed class AdminForgotPasswordCommandHandler(
    IUserAccountService userAccountService,
    IOtpService otpService,
    IEmailSender emailSender,
    IAppLocalizer localizer,
    ILogger<AdminForgotPasswordCommandHandler> logger) : IRequestHandler<AdminForgotPasswordCommand>
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
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Best-effort OTP invalidate failed after admin forgot-password email failure for {Email}",
                    request.Email);
            }

            throw;
        }

        logger.LogInformation("Admin forgot-password code sent for {Email}", request.Email);
    }
}
