using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Enterprise.Infrastructure.Persistence.Repositories;

public class DeviceTokenRepository(ApplicationDbContext context)
    : GenericRepository<DeviceToken>(context), IDeviceTokenRepository
{
    public async Task<DeviceToken?> GetByTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(d => d.Token == token, cancellationToken);
    }

    public async Task<IReadOnlyList<DeviceToken>> ListByUserIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
        {
            return [];
        }

        return await DbSet
            .Where(d => userIds.Contains(d.UserId))
            .ToListAsync(cancellationToken);
    }

    public async Task RemoveByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        await DbSet
            .Where(d => d.Token == token)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
