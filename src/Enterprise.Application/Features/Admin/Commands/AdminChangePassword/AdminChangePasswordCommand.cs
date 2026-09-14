using MediatR;

namespace Enterprise.Application.Features.Admin.Commands.AdminChangePassword;

public sealed record AdminChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest;
