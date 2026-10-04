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

    public Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        DbSet.CountAsync(cart => cart.UserId == userId && !cart.Provider.IsDeleted && cart.Items.Any(item => !item.Product.IsDeleted), cancellationToken);

    public async Task<IReadOnlyList<ShoppingCart>> ListPageByUserIdAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default) =>
        await DbSet.AsNoTracking().Where(cart => cart.UserId == userId && !cart.Provider.IsDeleted && cart.Items.Any(item => !item.Product.IsDeleted))
            .Include(cart => cart.Provider).Include(cart => cart.Items).ThenInclude(item => item.Product).ThenInclude(product => product.Category)
            .OrderByDescending(cart => cart.LastModifiedAtUtc ?? cart.CreatedAtUtc).ThenByDescending(cart => cart.Id)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

    public Task<int> CountUnitsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Context.Set<ShoppingCartItem>().Where(item => item.ShoppingCart.UserId == userId && !item.ShoppingCart.Provider.IsDeleted && !item.Product.IsDeleted)
            .SumAsync(item => item.Quantity, cancellationToken);

    public async Task ConsumePaidItemsAsync(Guid cartId, IReadOnlyDictionary<Guid, int> quantities, CancellationToken cancellationToken = default)
    {
        foreach (var (productId, quantity) in quantities)
        {
            var items = Context.Set<ShoppingCartItem>().Where(item => item.ShoppingCartId == cartId && item.ProductId == productId);
            await items.Where(item => item.Quantity <= quantity).ExecuteDeleteAsync(cancellationToken);
            await items.Where(item => item.Quantity > quantity)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Quantity, item => item.Quantity - quantity), cancellationToken);
        }
        await DbSet.Where(cart => cart.Id == cartId && !cart.Items.Any()).ExecuteDeleteAsync(cancellationToken);
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
