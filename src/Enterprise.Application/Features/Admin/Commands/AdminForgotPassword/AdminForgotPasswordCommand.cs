using MediatR;

namespace Enterprise.Application.Features.Admin.Commands.AdminForgotPassword;

public sealed record AdminForgotPasswordCommand(string Email) : IRequest;
