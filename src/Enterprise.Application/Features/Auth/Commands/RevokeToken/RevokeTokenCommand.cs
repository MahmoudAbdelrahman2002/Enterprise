using MediatR;

namespace Enterprise.Application.Features.Auth.Commands.RevokeToken;

public sealed record RevokeTokenCommand(string RefreshToken, string? IpAddress) : IRequest;
