using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Enterprise.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository(ApplicationDbContext context) : GenericRepository<Order>(context), IOrderRepository
{
    public Task<Order?> GetByCheckoutSessionIdAsync(
        string checkoutSessionId,
        CancellationToken cancellationToken = default) =>
        DbSet.AsNoTracking()
            .FirstOrDefaultAsync(o => o.StripeCheckoutSessionId == checkoutSessionId, cancellationToken);

    public Task<Order?> GetByCheckoutSessionIdForUserAsync(
        string checkoutSessionId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        DbSet.AsNoTracking()
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(
                o => o.StripeCheckoutSessionId == checkoutSessionId && o.UserId == userId,
                cancellationToken);

    public Task<Order?> GetTrackedByIdForUserAsync(
        Guid orderId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        DbSet.Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, cancellationToken);

    public Task<Order?> GetByIdWithItemsAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        DbSet.AsNoTracking()
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> SearchAsync(
        Guid? providerId,
        OrderStatus? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().Where(o => o.PreviousStatus != "Cancelled");

        if (providerId.HasValue)
        {
            query = query.Where(o => o.ProviderId == providerId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(o => o.OrderDateUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(o => o.OrderDateUtc <= toUtc.Value);
        }

        var page = pageNumber < 1 ? 1 : pageNumber;
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(o => o.OrderDateUtc)
            .ThenByDescending(o => o.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> SearchForUserAsync(Guid userId, Guid? providerId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().Where(order => order.UserId == userId);
        if (providerId.HasValue) query = query.Where(order => order.ProviderId == providerId.Value);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(order => order.OrderDateUtc).ThenByDescending(order => order.Id)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, count);
    }

    public async Task<IReadOnlyList<Order>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await DbSet.AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDateUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Order>> ListByUserAndProviderAsync(
        Guid userId,
        Guid providerId,
        CancellationToken cancellationToken = default) =>
        await DbSet.AsNoTracking()
            .Where(o => o.UserId == userId && o.ProviderId == providerId)
            .OrderByDescending(o => o.OrderDateUtc)
            .ToListAsync(cancellationToken);

    public Task<Order?> GetByIdForUserAsync(
        Guid orderId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        DbSet.AsNoTracking()
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<Order>> ListByProviderIdAsync(
        Guid providerId,
        CancellationToken cancellationToken = default) =>
        await DbSet.AsNoTracking()
            .Where(o => o.ProviderId == providerId && o.PreviousStatus != "Cancelled")
            .OrderByDescending(o => o.OrderDateUtc)
            .ToListAsync(cancellationToken);

    public Task<Order?> GetByIdForProviderAsync(
        Guid orderId,
        Guid providerId,
        CancellationToken cancellationToken = default) =>
        DbSet.AsNoTracking()
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.ProviderId == providerId, cancellationToken);

    public Task<Order?> GetTrackedByIdForProviderAsync(
        Guid orderId,
        Guid providerId,
        CancellationToken cancellationToken = default) =>
        DbSet.Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.ProviderId == providerId, cancellationToken);
}
