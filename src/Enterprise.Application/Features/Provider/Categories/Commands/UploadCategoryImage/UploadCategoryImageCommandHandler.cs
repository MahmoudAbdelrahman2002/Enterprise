using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Images;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Provider.Categories.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Categories.Commands.UploadCategoryImage;

public sealed class UploadCategoryImageCommandValidator : AbstractValidator<UploadCategoryImageCommand>
{
    public UploadCategoryImageCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.File).NotNull().WithMessage(_ => localizer[MessageKeys.Image.Required]);
        RuleFor(x => x.File)
            .Custom((file, context) =>
            {
                if (file is null) return;
                var error = ImageUploadRules.Validate(file.ContentType, file.Length);
                if (error is not null)
                {
                    context.AddFailure(localizer[error]);
                }
            });
    }
}

public sealed class UploadCategoryImageCommandHandler(
    IUnitOfWork unitOfWork,
    IProviderContext providerContext,
    IFileStorageService fileStorage,
    ICurrentCulture currentCulture,
    ILogger<UploadCategoryImageCommandHandler> logger) : IRequestHandler<UploadCategoryImageCommand, CategoryDetailDto>
{
    public async Task<CategoryDetailDto> Handle(UploadCategoryImageCommand request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);
        var category = await unitOfWork.Categories.GetByIdAndProviderIdAsync(request.Id, providerId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Category), request.Id);

        var extension = ImageUploadRules.ResolveExtension(request.File.ContentType);
        var folder = $"providers/{providerId}/categories/{category.Id}";
        var fileName = $"{Guid.NewGuid():N}{extension}";

        var url = await fileStorage.UploadAsync(
            request.File.Content,
            request.File.ContentType,
            folder,
            fileName,
            cancellationToken);

        var previous = category.ImageUrl;
        category.SetImageUrl(url);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try { await fileStorage.DeleteAsync(previous, cancellationToken); }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Best-effort blob delete failed for {Url}", previous);
        }

        logger.LogInformation(
            "Uploaded image for category {CategoryId} for provider {ProviderId}",
            category.Id,
            providerId);
        return category.ToDto(currentCulture.LanguageCode);
    }
}
