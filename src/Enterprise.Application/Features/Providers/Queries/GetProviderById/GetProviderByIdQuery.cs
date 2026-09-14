using Enterprise.Application.Features.Providers;
using MediatR;

namespace Enterprise.Application.Features.Providers.Queries.GetProviderById;

public sealed record GetProviderByIdQuery(Guid Id) : IRequest<ProviderDto>;
