using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Enterprise.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository(ApplicationDbContext context) : GenericRepository<Category>(context), ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> GetByProviderIdAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.ProviderId == providerId)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<Category?> GetByIdAndProviderIdAsync(Guid id, Guid providerId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(c => c.Id == id && c.ProviderId == providerId, cancellationToken);
    }
}
