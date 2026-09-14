using Enterprise.Application.Features.Providers;
using MediatR;

namespace Enterprise.Application.Features.Providers.Commands.SetProviderActive;

public sealed record SetProviderActiveCommand(Guid Id, bool IsActive) : IRequest<ProviderDto>;
