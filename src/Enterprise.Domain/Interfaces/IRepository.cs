using Enterprise.Domain.Common;
using Enterprise.Domain.Specifications;

namespace Enterprise.Domain.Interfaces;

/// <summary>
/// One generic implementation (<c>GenericRepository&lt;T&gt;</c> in Infrastructure) backs every
/// entity. Query shaping (filter/sort/include/page) is expressed through
/// <see cref="ISpecification{T}"/> objects rather than by growing this interface with
/// bespoke methods per feature - that is precisely the problem the Specification pattern
/// solves. Entity-specific lookups that genuinely need a hand-written query (e.g.
/// "find user by normalized email", which needs a case-insensitive comparison a generic
/// specification would express awkwardly) live on the narrow, entity-specific repository
/// interfaces (<see cref="IUserRepository"/> etc.) that extend this one.
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
    Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    void Add(T entity);
    void Update(T entity);
    void Remove(T entity);
}
