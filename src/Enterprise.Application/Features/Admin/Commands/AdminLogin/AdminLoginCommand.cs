using Enterprise.Application.Features.Auth;
using MediatR;

namespace Enterprise.Application.Features.Admin.Commands.AdminLogin;

public sealed record AdminLoginCommand(string Email, string Password, string? IpAddress) : IRequest<AuthResponseDto>;
