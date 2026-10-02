using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Provider.Categories.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Categories.Queries.GetCategoriesList;

public sealed class GetCategoriesListQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentCulture currentCulture,
    IProviderContext providerContext,
    ILogger<GetCategoriesListQueryHandler> logger)
    : IRequestHandler<GetCategoriesListQuery, IReadOnlyList<CategoryDetailDto>>
{
    public async Task<IReadOnlyList<CategoryDetailDto>> Handle(
        GetCategoriesListQuery request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);
        var currentLanguage = currentCulture.LanguageCode;

        var categories = await unitOfWork.Categories.GetByProviderIdAsync(providerId, cancellationToken);

        if (request.IsActive.HasValue)
        {
            categories = categories.Where(c => c.IsActive == request.IsActive.Value).ToList();
        }

        var result = categories
            .Select(c => c.ToDto(currentLanguage, includeTranslations: false))
            .ToList();

        logger.LogInformation("Listed {Count} categories for provider {ProviderId}", result.Count, providerId);
        return result;
    }
}
