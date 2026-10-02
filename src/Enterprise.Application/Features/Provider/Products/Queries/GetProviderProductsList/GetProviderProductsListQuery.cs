using Enterprise.Application.Features.Provider.Products.DTOs;
using Enterprise.Domain.Enums;
using MediatR;

namespace Enterprise.Application.Features.Provider.Products.Queries.GetProviderProductsList;

public sealed record GetProviderProductsListQuery(Guid CategoryId, ProductStatus? Status = null)
    : IRequest<IReadOnlyList<ProductListItemDto>>;
