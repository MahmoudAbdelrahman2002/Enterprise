using Enterprise.Domain.Entities;

namespace Enterprise.Domain.Interfaces;

public interface IMarketplaceServiceRepository : IRepository<MarketplaceService>
{
    Task<MarketplaceService?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> HasLinkedProvidersAsync(Guid serviceId, CancellationToken cancellationToken = default);
}
