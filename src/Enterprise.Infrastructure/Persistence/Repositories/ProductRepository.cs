using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Enterprise.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository(ApplicationDbContext context) : GenericRepository<Product>(context), IProductRepository
{
    public override async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet.Include(p => p.Translations).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(p => p.Sku == sku, cancellationToken);
}
