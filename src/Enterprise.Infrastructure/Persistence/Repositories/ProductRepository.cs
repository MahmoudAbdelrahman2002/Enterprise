using Enterprise.Domain.Entities;
using Enterprise.Domain.Enums;
using Enterprise.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Enterprise.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository(ApplicationDbContext context)
    : GenericRepository<Product>(context), IProductRepository
{
    public async Task<IReadOnlyList<Product>> ListByProviderIdAsync(
        Guid providerId,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .Where(product => product.Category.ProviderId == providerId)
            .OrderBy(product => product.Sku)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Product>> GetAllByCategoryIdAndProviderIdAsync(
        Guid categoryId,
        Guid providerId,
        ProductStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(p =>
            p.CategoryId == categoryId && p.Category.ProviderId == providerId);

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<Product?> GetByIdAndCategoryIdAndProviderIdAsync(
        Guid id,
        Guid categoryId,
        Guid providerId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(
                p => p.Id == id
                     && p.CategoryId == categoryId
                     && p.Category.ProviderId == providerId,
                cancellationToken);
    }

    public Task<int> CountByCategoryIdAndProviderIdAsync(
        Guid categoryId,
        Guid providerId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.CountAsync(
            p => p.CategoryId == categoryId && p.Category.ProviderId == providerId,
            cancellationToken);
    }

    public Task<bool> ExistsBySkuAsync(
        string sku,
        Guid providerId,
        Guid? excludeProductId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = sku.Trim();
        return DbSet.AnyAsync(
            p => p.Sku == normalized
                 && p.Category.ProviderId == providerId
                 && (!excludeProductId.HasValue || p.Id != excludeProductId.Value),
            cancellationToken);
    }

    public async Task SoftDeleteByCategoryIdAndProviderIdAsync(
        Guid categoryId,
        Guid providerId,
        string deletedBy,
        DateTime deletedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await DbSet
            .Where(p =>
                p.CategoryId == categoryId
                && p.Category.ProviderId == providerId
                && !p.IsDeleted)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(p => p.IsDeleted, true)
                    .SetProperty(p => p.DeletedAtUtc, deletedAtUtc)
                    .SetProperty(p => p.DeletedBy, deletedBy),
                cancellationToken);
    }
}
