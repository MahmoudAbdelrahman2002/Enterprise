using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Provider.Categories.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Categories.Commands.SetCategoryActive;

public sealed class SetCategoryActiveCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentCulture currentCulture,
    IProviderContext providerContext,
    ILogger<SetCategoryActiveCommandHandler> logger)
    : IRequestHandler<SetCategoryActiveCommand, CategoryDetailDto>
{
    public async Task<CategoryDetailDto> Handle(
        SetCategoryActiveCommand request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);

        var category = await unitOfWork.Categories.GetByIdAndProviderIdAsync(request.Id, providerId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Category), request.Id);

        category.SetActive(request.IsActive);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Set category {CategoryId} active={IsActive} for provider {ProviderId}",
            category.Id,
            request.IsActive,
            providerId);
        return category.ToDto(currentCulture.LanguageCode);
    }
}
