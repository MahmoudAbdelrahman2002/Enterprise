using MediatR;

namespace Enterprise.Application.Features.Notifications.Commands.UnregisterDeviceToken;

public sealed record UnregisterDeviceTokenCommand(string Token) : IRequest;
