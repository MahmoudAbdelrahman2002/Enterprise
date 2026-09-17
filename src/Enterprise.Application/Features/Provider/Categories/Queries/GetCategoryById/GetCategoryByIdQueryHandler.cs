using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Provider.Categories.DTOs;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Provider.Categories.Queries.GetCategoryById;

public class GetCategoryByIdQueryHandler(IUnitOfWork unitOfWork,ICurrentCulture currentCulture,ICurrentUserService currentUserService

) : IRequestHandler<GetCategoryByIdQuery, CategoryDetailDto>
{
    public async Task<CategoryDetailDto> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var providerId = await ResolveProviderIdAsync(cancellationToken);
        var currentLanguage = currentCulture.LanguageCode;
        var category = await unitOfWork.Categories.GetByIdAsync(request.Id, cancellationToken);
        if (category is null || category.ProviderId != providerId)
        {
            throw NotFoundException.For(nameof(Enterprise.Domain.Entities.Category), request.Id);
        }
        return category.ToDto(currentLanguage);
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