using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Provider.Categories;
using Enterprise.Application.Features.Provider.Categories.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Admin.Providers.Queries.GetAdminProviderCategories;

public sealed class GetAdminProviderCategoriesQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentCulture currentCulture)
    : IRequestHandler<GetAdminProviderCategoriesQuery, IReadOnlyList<CategoryDetailDto>>
{
    public async Task<IReadOnlyList<CategoryDetailDto>> Handle(
        GetAdminProviderCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        _ = await unitOfWork.Providers.GetByIdAsync(request.ProviderId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Provider), request.ProviderId);

        var categories = await unitOfWork.Categories.GetByProviderIdAsync(request.ProviderId, cancellationToken);
        return categories
            .Select(category => category.ToDto(currentCulture.LanguageCode, includeTranslations: false))
            .ToList();
    }
}
