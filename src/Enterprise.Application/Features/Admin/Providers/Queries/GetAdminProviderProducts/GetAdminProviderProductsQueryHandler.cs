using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Provider.Products;
using Enterprise.Application.Features.Provider.Products.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Admin.Providers.Queries.GetAdminProviderProducts;

public sealed class GetAdminProviderProductsQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentCulture currentCulture)
    : IRequestHandler<GetAdminProviderProductsQuery, IReadOnlyList<ProductListItemDto>>
{
    public async Task<IReadOnlyList<ProductListItemDto>> Handle(
        GetAdminProviderProductsQuery request,
        CancellationToken cancellationToken)
    {
        _ = await unitOfWork.Providers.GetByIdAsync(request.ProviderId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Provider), request.ProviderId);

        var products = await unitOfWork.Products.ListByProviderIdAsync(request.ProviderId, cancellationToken);
        return products
            .Select(product => product.ToListItemDto(currentCulture.LanguageCode))
            .ToList();
    }
}
