using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Enums;
using MediatR;

namespace Enterprise.Application.Features.Provider.Commands.ProviderResetPassword;

public sealed class ProviderResetPasswordCommandHandler(
    IUserAccountService userAccountService,
    IOtpService otpService) : IRequestHandler<ProviderResetPasswordCommand>
{
    public async Task Handle(ProviderResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var valid = await otpService.VerifyAndConsumeAsync(
            request.Email, OtpPurpose.ResetPassword, request.Otp, cancellationToken);
        if (!valid)
        {
            throw new AuthenticationFailedException(MessageKeys.Auth.InvalidOrExpiredResetCode);
        }

        var user = await userAccountService.FindByEmailAsync(request.Email, cancellationToken);
        if (user is null || user.UserType != UserType.Provider)
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
