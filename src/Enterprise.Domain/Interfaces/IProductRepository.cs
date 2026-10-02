using Enterprise.Domain.Entities;
using Enterprise.Domain.Enums;

namespace Enterprise.Domain.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<IReadOnlyList<Product>> ListByProviderIdAsync(
        Guid providerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> GetAllByCategoryIdAndProviderIdAsync(
        Guid categoryId,
        Guid providerId,
        ProductStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAndCategoryIdAndProviderIdAsync(
        Guid id,
        Guid categoryId,
        Guid providerId,
        CancellationToken cancellationToken = default);

    Task<int> CountByCategoryIdAndProviderIdAsync(
        Guid categoryId,
        Guid providerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Provider-scoped SKU check via Category.ProviderId (products do not store ProviderId).
    /// </summary>
    Task<bool> ExistsBySkuAsync(
        string sku,
        Guid providerId,
        Guid? excludeProductId = null,
        CancellationToken cancellationToken = default);

    Task SoftDeleteByCategoryIdAndProviderIdAsync(
        Guid categoryId,
        Guid providerId,
        string deletedBy,
        DateTime deletedAtUtc,
        CancellationToken cancellationToken = default);
}
