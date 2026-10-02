using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Enterprise.Infrastructure.Persistence.Repositories;

public class CartRepository(ApplicationDbContext context) : GenericRepository<ShoppingCart>(context), ICartRepository
{
    public async Task<ShoppingCart?> GetCartByProviderIdAndUserIdAsync(
        Guid providerId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(c => c.Provider)
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(
                c => c.ProviderId == providerId && c.UserId == userId,
                cancellationToken);
    }

    public async Task<ShoppingCart?> GetByIdWithItemsAsync(
        Guid cartId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(c => c.Id == cartId, cancellationToken);
    }

    public async Task<IReadOnlyList<ShoppingCart>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(c => c.Provider)
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Category)
            .Where(c => c.UserId == userId && c.Items.Count > 0)
            .OrderByDescending(c => c.LastModifiedAtUtc ?? c.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteByIdAsync(Guid cartId, CancellationToken cancellationToken = default)
    {
        await Context.Set<ShoppingCartItem>()
            .Where(i => i.ShoppingCartId == cartId)
            .ExecuteDeleteAsync(cancellationToken);

        await Context.Set<ShoppingCart>()
            .Where(c => c.Id == cartId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
