using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Enterprise.Infrastructure.Persistence.Repositories;

public sealed class ProviderRepository(ApplicationDbContext context)
    : GenericRepository<Provider>(context), IProviderRepository
{
    public Task<Provider?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public Task<bool> ExistsForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(p => p.UserId == userId, cancellationToken);
}
