using Enterprise.Application.Common.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Client.Providers.Queries.GetClientProviderById;

public sealed record GetClientProviderByIdQuery(Guid ProviderId) : IRequest<ClientProviderListItemDto>;
