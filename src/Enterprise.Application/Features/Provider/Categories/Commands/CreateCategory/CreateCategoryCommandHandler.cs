using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Provider.Categories.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Provider.Categories.Commands.CreateCategory;

public sealed class CreateCategoryCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ICurrentCulture currentCulture)
    : IRequestHandler<CreateCategoryCommand, CategoryDetailDto>
{
    public async Task<CategoryDetailDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var providerId = await ResolveProviderIdAsync(cancellationToken);

        var category = new Category(
            providerId: providerId,
            displayOrder: request.DisplayOrder,
            isActive: request.IsActive);
            try
        {
        category.ApplyLocalizedContent(request.Name, request.Description);
        unitOfWork.Categories.Add(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        }
            catch (System.Exception)
            {
                unitOfWork.Categories.Remove(category);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw;
            }
        

        return category.ToDto(currentCulture.LanguageCode);
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
