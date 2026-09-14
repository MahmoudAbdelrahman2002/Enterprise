using MediatR;

namespace Enterprise.Application.Features.Admin.Commands.AdminResetPassword;

public sealed record AdminResetPasswordCommand(string Email, string Otp, string NewPassword) : IRequest;
