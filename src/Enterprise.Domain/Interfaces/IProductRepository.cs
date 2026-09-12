using Enterprise.Domain.Entities;

namespace Enterprise.Domain.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken = default);
}
