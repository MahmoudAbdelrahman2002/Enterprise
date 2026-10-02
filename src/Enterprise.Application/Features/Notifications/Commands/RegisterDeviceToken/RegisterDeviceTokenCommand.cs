using MediatR;

namespace Enterprise.Application.Features.Notifications.Commands.RegisterDeviceToken;

public sealed record RegisterDeviceTokenCommand(string Token, string? Platform = null) : IRequest;
