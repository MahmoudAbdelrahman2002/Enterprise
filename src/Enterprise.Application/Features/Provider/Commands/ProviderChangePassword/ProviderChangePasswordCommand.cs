using MediatR;

namespace Enterprise.Application.Features.Provider.Commands.ProviderChangePassword;

public sealed record ProviderChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest;
