using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Providers;
using Enterprise.Domain.Entities;
using MediatR;

namespace Enterprise.Application.Features.Provider.Store.Queries.GetProviderStore;

public sealed class GetProviderStoreQueryHandler(
    IProviderContext providerContext,
    IProviderAdminQueryService providerAdminQueryService)
    : IRequestHandler<GetProviderStoreQuery, ProviderStoreDto>
{
    public async Task<ProviderStoreDto> Handle(GetProviderStoreQuery request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);
        var detail = await providerAdminQueryService.GetByIdAsync(providerId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Provider), providerId);

        return ProviderStoreDto.From(detail.ToDto());
    }
}
