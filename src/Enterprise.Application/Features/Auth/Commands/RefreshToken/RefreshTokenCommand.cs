using Enterprise.Domain.Enums;
using MediatR;

namespace Enterprise.Application.Features.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken, string? IpAddress, UserType ExpectedUserType)
    : IRequest<AuthResponseDto>;
