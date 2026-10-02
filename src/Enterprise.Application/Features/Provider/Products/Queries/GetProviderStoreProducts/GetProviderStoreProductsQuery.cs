using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Provider.Products.DTOs;
using Enterprise.Domain.Enums;
using MediatR;

namespace Enterprise.Application.Features.Provider.Products.Queries.GetProviderStoreProducts;

public sealed record GetProviderStoreProductsQuery : PaginationParams, IRequest<PagedResult<ProductListItemDto>>
{
    public Guid? CategoryId { get; init; }
    public ProductStatus? Status { get; init; }
    public string? SearchTerm { get; init; }
}
