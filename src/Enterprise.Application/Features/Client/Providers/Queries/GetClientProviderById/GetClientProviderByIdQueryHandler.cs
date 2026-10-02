using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Entities;
using MediatR;

namespace Enterprise.Application.Features.Client.Providers.Queries.GetClientProviderById;

public sealed class GetClientProviderByIdQueryHandler(IClientProviderQueryService clientProviderQueryService)
    : IRequestHandler<GetClientProviderByIdQuery, ClientProviderListItemDto>
{
    public async Task<ClientProviderListItemDto> Handle(
        GetClientProviderByIdQuery request,
        CancellationToken cancellationToken) =>
        await clientProviderQueryService.GetByIdAsync(request.ProviderId, cancellationToken)
        ?? throw NotFoundException.For(nameof(Provider), request.ProviderId);
}
