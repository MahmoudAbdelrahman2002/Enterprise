using Enterprise.Domain.Entities;

namespace Enterprise.Domain.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    Task<IReadOnlyList<Category>> GetByProviderIdAsync(Guid providerId, CancellationToken cancellationToken = default);
    Task<Category?> GetByIdAndProviderIdAsync(Guid id, Guid providerId, CancellationToken cancellationToken = default);

}
