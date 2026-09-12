using Enterprise.Domain.Entities;

namespace Enterprise.Domain.Interfaces;

public interface IApiKeyRepository : IRepository<ApiKey>
{
    Task<ApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken = default);
}
