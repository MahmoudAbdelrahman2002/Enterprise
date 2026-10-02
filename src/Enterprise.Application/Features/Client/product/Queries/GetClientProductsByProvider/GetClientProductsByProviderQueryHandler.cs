using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Client.product;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Enums;
using Enterprise.Domain.Interfaces;
using Enterprise.Domain.Specifications.Services;
using MediatR;

namespace Enterprise.Application.Features.Client.product.Queries.GetClientProductsByProvider;

public sealed class GetClientProductsByProviderQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentCulture currentCulture,
    IClientProviderQueryService clientProviderQueryService)
    : IRequestHandler<GetClientProductsByProviderQuery, PagedResult<ClientProductDto>>
{
    public async Task<PagedResult<ClientProductDto>> Handle(
        GetClientProductsByProviderQuery request,
        CancellationToken cancellationToken)
    {
        _ = await clientProviderQueryService.GetByIdAsync(request.ProviderId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Provider), request.ProviderId);

        if (request.CategoryId.HasValue)
        {
            var category = await unitOfWork.Categories.GetByIdAndProviderIdAsync(
                request.CategoryId.Value,
                request.ProviderId,
                cancellationToken);
            if (category is null || !category.IsActive)
            {
                throw NotFoundException.For(nameof(Category), request.CategoryId.Value);
            }
        }

        var language = currentCulture.LanguageCode;
        const ProductStatus status = ProductStatus.Active;
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;

        var countSpec = ProductFilterSpecification.ForProviderCount(
            request.ProviderId, request.CategoryId, request.SearchTerm, status, language);
        var totalCount = await unitOfWork.Products.CountAsync(countSpec, cancellationToken);

        var pageSpec = ProductFilterSpecification.ForProviderPage(
            request.ProviderId,
            request.CategoryId,
            request.SearchTerm,
            status,
            pageNumber,
            request.PageSize,
            language);
        var products = await unitOfWork.Products.ListAsync(pageSpec, cancellationToken);
        var items = products.Select(product => product.ProductToDto(language)).ToList();

        return new PagedResult<ClientProductDto>(items, totalCount, pageNumber, request.PageSize);
    }
}
