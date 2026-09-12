using System.Linq.Expressions;

namespace Enterprise.Domain.Specifications;

/// <summary>
/// Describes "which rows, in what shape" as data instead of scattered LINQ calls. The
/// generic repository stays generic (one implementation, no per-feature subclass explosion)
/// because all the feature-specific query shaping - filter predicates, sorting, includes,
/// paging - is captured by the specification object handed to it, and evaluated in one place
/// by <see cref="SpecificationEvaluator{T}"/>.
/// </summary>
public interface ISpecification<T>
{
    Expression<Func<T, bool>>? Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    Expression<Func<T, object>>? OrderBy { get; }
    Expression<Func<T, object>>? OrderByDescending { get; }

    int Skip { get; }
    int Take { get; }
    bool IsPagingEnabled { get; }
}
