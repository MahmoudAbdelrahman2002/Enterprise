using MediatR;

namespace Enterprise.Application.Features.Provider.Store.Queries.GetProviderStore;

public sealed record GetProviderStoreQuery : IRequest<ProviderStoreDto>;
