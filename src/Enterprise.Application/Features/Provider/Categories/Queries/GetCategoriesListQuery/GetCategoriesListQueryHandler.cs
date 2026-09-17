using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Provider.Categories.DTOs;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Provider.Categories.Queries.GetCategoriesListQuery;

public class GetCategoriesListQueryHandler(IUnitOfWork unitOfWork,ICurrentCulture currentCulture,ICurrentUserService currentUserService) : IRequestHandler<GetCategoriesListQuery, IReadOnlyList<CategoryDetailDto>>
{
    public async Task<IReadOnlyList<CategoryDetailDto>> Handle(GetCategoriesListQuery request, CancellationToken cancellationToken)
    {
        var providerId = await ResolveProviderIdAsync(cancellationToken);
        var currentLanguage = currentCulture.LanguageCode;
        var categories = await unitOfWork.Categories.GetAllAsync(cancellationToken);
        categories = categories.Where(c => c.ProviderId == providerId).ToList();
        return categories.Select(c => c.ToDto(currentLanguage,includeTranslations: false)).ToList();
    }
      private async Task<Guid> ResolveProviderIdAsync(CancellationToken cancellationToken)
    {
        if (currentUserService.ProviderId.HasValue)
        {
            return currentUserService.ProviderId.Value;
        }

        var userId = currentUserService.UserId ?? throw new ForbiddenAccessException();
        var provider = await unitOfWork.Providers.GetByUserIdAsync(userId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Enterprise.Domain.Entities.Provider), userId);

        return provider.Id;
    }
}