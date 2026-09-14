using MediatR;

namespace Enterprise.Application.Features.Providers.Commands.DeleteProvider;

public sealed record DeleteProviderCommand(Guid Id) : IRequest;
