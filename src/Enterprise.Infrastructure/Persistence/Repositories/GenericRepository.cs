using Enterprise.Domain.Common;
using Enterprise.Domain.Interfaces;
using Enterprise.Domain.Specifications;
using Enterprise.Infrastructure.Persistence.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Enterprise.Infrastructure.Persistence.Repositories;

/// <summary>
/// The ONE repository implementation for every entity. Entity-specific repositories
/// (<see cref="ProductRepository"/> etc.) inherit this for the generic CRUD/specification
/// surface and add only the handful of methods that genuinely can't be expressed as a
/// specification (e.g. a case-insensitive "does this email exist" check).
/// </summary>
public class GenericRepository<T>(ApplicationDbContext context) : IRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext Context = context;
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet.FindAsync([id], cancellationToken);

    public async Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default) =>
        await ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default) =>
        await DbSet.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default) =>
        await ApplySpecification(specification).AsNoTracking().ToListAsync(cancellationToken);

    public async Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default) =>
        await ApplySpecification(specification).CountAsync(cancellationToken);

    public async Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default) =>
        await ApplySpecification(specification).AnyAsync(cancellationToken);

    public void Add(T entity) => DbSet.Add(entity);

    public void Update(T entity) => DbSet.Update(entity);

    public void Remove(T entity) => DbSet.Remove(entity);

    private IQueryable<T> ApplySpecification(ISpecification<T> specification) =>
        SpecificationEvaluator<T>.GetQuery(DbSet.AsQueryable(), specification);
}
