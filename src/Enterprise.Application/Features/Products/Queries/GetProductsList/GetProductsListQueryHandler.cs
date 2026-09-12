using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Domain.Interfaces;
using Enterprise.Domain.Specifications.Products;
using MediatR;

namespace Enterprise.Application.Features.Products.Queries.GetProductsList;

public sealed class GetProductsListQueryHandler(IUnitOfWork unitOfWork, ICurrentCulture culture)
    : IRequestHandler<GetProductsListQuery, PagedResult<ProductDto>>
{
    public async Task<PagedResult<ProductDto>> Handle(GetProductsListQuery request, CancellationToken cancellationToken)
    {
        var language = culture.LanguageCode;
        var countSpec = ProductFilterSpecification.ForCount(
            request.SearchTerm, request.Category, request.Status, language);
        var totalCount = await unitOfWork.Products.CountAsync(countSpec, cancellationToken);

        var pageSpec = ProductFilterSpecification.ForPage(
            request.SearchTerm, request.Category, request.Status,
            request.SortBy, request.SortDescending, request.PageNumber, request.PageSize, language);
        var products = await unitOfWork.Products.ListAsync(pageSpec, cancellationToken);

        var items = products.Select(product => product.ToDto(language)).ToList();

        return new PagedResult<ProductDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
