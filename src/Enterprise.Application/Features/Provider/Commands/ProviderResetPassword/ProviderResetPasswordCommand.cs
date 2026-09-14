using MediatR;

namespace Enterprise.Application.Features.Provider.Commands.ProviderResetPassword;

public sealed record ProviderResetPasswordCommand(string Email, string Otp, string NewPassword) : IRequest;
