using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Enterprise.Infrastructure.Persistence.Repositories;

public sealed class MarketplaceServiceRepository(ApplicationDbContext context)
    : GenericRepository<MarketplaceService>(context), IMarketplaceServiceRepository
{
    public Task<MarketplaceService?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToLowerInvariant();
        return DbSet.Include(s => s.Translations)
            .FirstOrDefaultAsync(s => s.Code == normalized, cancellationToken);
    }

    public Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToLowerInvariant();
        var query = DbSet.Where(s => s.Code == normalized);
        if (excludeId.HasValue)
        {
            query = query.Where(s => s.Id != excludeId.Value);
        }
        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> HasLinkedProvidersAsync(Guid serviceId, CancellationToken cancellationToken = default) =>
        Context.Set<Provider>().AnyAsync(p => p.ServiceId == serviceId, cancellationToken);
}


