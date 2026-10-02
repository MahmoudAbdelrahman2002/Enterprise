using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Provider.Products.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using Enterprise.Domain.Specifications.Services;
using MediatR;

namespace Enterprise.Application.Features.Provider.Products.Queries.GetProviderStoreProducts;

public sealed class GetProviderStoreProductsQueryHandler(
    IUnitOfWork unitOfWork,
    IProviderContext providerContext,
    ICurrentCulture currentCulture)
    : IRequestHandler<GetProviderStoreProductsQuery, PagedResult<ProductListItemDto>>
{
    public async Task<PagedResult<ProductListItemDto>> Handle(
        GetProviderStoreProductsQuery request,
        CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);

        if (request.CategoryId.HasValue)
        {
            _ = await unitOfWork.Categories.GetByIdAndProviderIdAsync(
                    request.CategoryId.Value,
                    providerId,
                    cancellationToken)
                ?? throw NotFoundException.For(nameof(Category), request.CategoryId.Value);
        }

        var language = currentCulture.LanguageCode;
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;

        var countSpec = ProductFilterSpecification.ForProviderCatalogCount(
            providerId,
            request.CategoryId,
            request.SearchTerm,
            request.Status,
            language);
        var totalCount = await unitOfWork.Products.CountAsync(countSpec, cancellationToken);

        var pageSpec = ProductFilterSpecification.ForProviderCatalogPage(
            providerId,
            request.CategoryId,
            request.SearchTerm,
            request.Status,
            pageNumber,
            request.PageSize,
            language);
        var products = await unitOfWork.Products.ListAsync(pageSpec, cancellationToken);
        var items = products.Select(product => product.ToListItemDto(language)).ToList();

        return new PagedResult<ProductListItemDto>(items, totalCount, pageNumber, request.PageSize);
    }
}
