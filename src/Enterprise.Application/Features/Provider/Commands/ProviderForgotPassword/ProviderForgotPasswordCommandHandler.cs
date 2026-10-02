using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Commands.ProviderForgotPassword;

public sealed class ProviderForgotPasswordCommandHandler(
    IUserAccountService userAccountService,
    IOtpService otpService,
    IEmailSender emailSender,
    IAppLocalizer localizer,
    ILogger<ProviderForgotPasswordCommandHandler> logger) : IRequestHandler<ProviderForgotPasswordCommand>
{
    public async Task Handle(ProviderForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userAccountService.FindByEmailAsync(request.Email, cancellationToken);
        if (user is null || user.UserType != UserType.Provider || !user.IsActive)
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
                    "Best-effort OTP invalidate failed after provider forgot-password email failure for {Email}",
                    request.Email);
            }

            throw;
        }

        logger.LogInformation("Provider forgot-password code sent for {Email}", request.Email);
    }
}
