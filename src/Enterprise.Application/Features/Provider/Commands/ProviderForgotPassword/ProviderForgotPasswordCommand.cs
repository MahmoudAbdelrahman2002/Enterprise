using MediatR;

namespace Enterprise.Application.Features.Provider.Commands.ProviderForgotPassword;

public sealed record ProviderForgotPasswordCommand(string Email) : IRequest;
