using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Enterprise.Infrastructure.Persistence.Repositories;

public sealed class ApiKeyRepository(ApplicationDbContext context) : GenericRepository<ApiKey>(context), IApiKeyRepository
{
    public Task<ApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(k => k.KeyHash == keyHash, cancellationToken);
}
