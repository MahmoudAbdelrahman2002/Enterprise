using Enterprise.Application.Common.Behaviors;
using Enterprise.Application.Common.Models;
using Enterprise.Domain.Common;
using Enterprise.Domain.Enums;
using MediatR;

namespace Enterprise.Application.Features.Products.Queries.GetProductsList;

/// <summary>
/// Model-bound directly from the query string (<c>[FromQuery] GetProductsListQuery</c> on the
/// controller action) - this record IS the contract for search/filter/sort/paging, so there is
/// nothing controller-side to translate.
/// </summary>
public sealed record GetProductsListQuery : PaginationParams, IRequest<PagedResult<ProductDto>>, ICacheableQuery
{
    public string? SearchTerm { get; init; }
    public string? Category { get; init; }
    public ProductStatus? Status { get; init; }
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }

    public string CacheKey =>
        $"products:list:{SupportedLanguages.Current}:{SearchTerm}:{Category}:{Status}:{SortBy}:{SortDescending}:{PageNumber}:{PageSize}";

    public TimeSpan? Expiration => TimeSpan.FromMinutes(2);
}
