using Enterprise.Application.Features.Auth;
using MediatR;

namespace Enterprise.Application.Features.Provider.Commands.ProviderLogin;

public sealed record ProviderLoginCommand(string Email, string Password, string? IpAddress) : IRequest<AuthResponseDto>;
