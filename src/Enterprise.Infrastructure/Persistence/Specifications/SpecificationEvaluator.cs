using Enterprise.Domain.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Enterprise.Infrastructure.Persistence.Specifications;

/// <summary>
/// Lives in Infrastructure, not Domain, specifically because <c>.Include()</c> is an
/// EF Core LINQ extension, not a generic <see cref="IQueryable{T}"/> method - the specification
/// *shape* (<see cref="ISpecification{T}"/>) is EF-agnostic and belongs in Domain, but turning
/// that shape into an actual query against a <c>DbSet&lt;T&gt;</c> is unavoidably EF-specific.
/// </summary>
public static class SpecificationEvaluator<T> where T : class
{
    public static IQueryable<T> GetQuery(IQueryable<T> inputQuery, ISpecification<T> specification)
    {
        var query = inputQuery;

        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));

        if (specification.OrderBy is not null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending is not null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        if (specification.IsPagingEnabled)
        {
            query = query.Skip(specification.Skip).Take(specification.Take);
        }

        return query;
    }
}
