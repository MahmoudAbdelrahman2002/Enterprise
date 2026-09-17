using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Provider.Categories.DTOs;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Provider.Categories.Commands.UpdateCategoryCommand;

public class UpdateCategoryCommandHandler(IUnitOfWork unitOfWork,ICurrentCulture currentCulture,ICurrentUserService currentUserService)
 : IRequestHandler<UpdateCategoryCommand, CategoryDetailDto>
{
    public async Task<CategoryDetailDto> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var currentLanguage = currentCulture.LanguageCode;
        var providerId = await ResolveProviderIdAsync(cancellationToken);
        var categoryId = request.Id;
        if (categoryId != request.Command.Id)
        {
            throw NotFoundException.For(nameof(Enterprise.Domain.Entities.Category), request.Id);
        }
        var category = await unitOfWork.Categories.GetByIdAsync(categoryId, cancellationToken);
         if (category is null || category.ProviderId != providerId)
          {
        throw NotFoundException.For(nameof(Enterprise.Domain.Entities.Category), request.Id);
    }
        category.UpdateDetails(request.Command.DisplayOrder ?? category.DisplayOrder);
       category.ApplyLocalizedContent(request.Command.Name, request.Command.Description);
        category.SetActive(request.Command.IsActive ?? category.IsActive);
        await unitOfWork.SaveChangesAsync(cancellationToken);
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