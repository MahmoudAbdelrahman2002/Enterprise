using Enterprise.Domain.Entities;

namespace Enterprise.Domain.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetByCheckoutSessionIdAsync(string checkoutSessionId, CancellationToken cancellationToken = default);

    Task<Order?> GetByCheckoutSessionIdForUserAsync(
        string checkoutSessionId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Order?> GetTrackedByIdForUserAsync(
        Guid orderId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Order?> GetByIdWithItemsAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Order> Items, int TotalCount)> SearchAsync(
        Guid? providerId,
        OrderStatus? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> ListByUserAndProviderAsync(
        Guid userId,
        Guid providerId,
        CancellationToken cancellationToken = default);

    Task<Order?> GetByIdForUserAsync(Guid orderId, Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> ListByProviderIdAsync(Guid providerId, CancellationToken cancellationToken = default);

    Task<Order?> GetByIdForProviderAsync(Guid orderId, Guid providerId, CancellationToken cancellationToken = default);

    Task<Order?> GetTrackedByIdForProviderAsync(
        Guid orderId,
        Guid providerId,
        CancellationToken cancellationToken = default);
}
