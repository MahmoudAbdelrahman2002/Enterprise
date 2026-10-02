using Enterprise.Application.Features.Provider.Products.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Admin.Providers.Queries.GetAdminProviderProducts;

public sealed record GetAdminProviderProductsQuery(Guid ProviderId) : IRequest<IReadOnlyList<ProductListItemDto>>;
