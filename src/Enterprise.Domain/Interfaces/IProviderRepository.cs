using Enterprise.Domain.Entities;

namespace Enterprise.Domain.Interfaces;

public interface IProviderRepository : IRepository<Provider>
{
    Task<Provider?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
