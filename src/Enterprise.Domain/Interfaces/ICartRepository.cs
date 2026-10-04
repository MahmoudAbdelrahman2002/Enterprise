using Enterprise.Domain.Entities;

namespace Enterprise.Domain.Interfaces;

public interface ICartRepository : IRepository<ShoppingCart>
{
    Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShoppingCart>> ListPageByUserIdAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountUnitsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ShoppingCart?> GetCartByProviderIdAndUserIdAsync(Guid providerId, Guid userId, CancellationToken cancellationToken = default);
    Task<ShoppingCart?> GetByIdWithItemsAsync(Guid cartId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShoppingCart>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the cart and its items by key. Uses a direct delete so soft-delete filters
    /// on Product/Provider are not added to the DELETE and do not throw a concurrency error.
    /// </summary>
    Task DeleteByIdAsync(Guid cartId, CancellationToken cancellationToken = default);
    Task ConsumePaidItemsAsync(Guid cartId, IReadOnlyDictionary<Guid, int> quantities, CancellationToken cancellationToken = default);
}
