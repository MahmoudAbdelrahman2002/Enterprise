using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Images;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Provider.Products.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Products.Commands.UploadProductImage;

public sealed class UploadProductImageCommandValidator : AbstractValidator<UploadProductImageCommand>
{
    public UploadProductImageCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.File).NotNull().WithMessage(_ => localizer[MessageKeys.Image.Required]);
        RuleFor(x => x.File)
            .CustomAsync(async (file, context, cancellationToken) =>
            {
                if (file is null) return;
                var error = await ImageUploadRules.ValidateContentAsync(file, cancellationToken);
                if (error is not null)
                {
                    context.AddFailure(localizer[error]);
                }
            });
    }
}

public sealed class UploadProductImageCommandHandler(
    IUnitOfWork unitOfWork,
    IProviderContext providerContext,
    IFileStorageService fileStorage,
    ICurrentCulture currentCulture,
    ILogger<UploadProductImageCommandHandler> logger) : IRequestHandler<UploadProductImageCommand, ProductDetailDto>
{
    public async Task<ProductDetailDto> Handle(UploadProductImageCommand request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);

        _ = await unitOfWork.Categories.GetByIdAndProviderIdAsync(request.CategoryId, providerId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Category), request.CategoryId);

        var product = await unitOfWork.Products.GetByIdAndCategoryIdAndProviderIdAsync(
                request.Id, request.CategoryId, providerId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Product), request.Id);

        var extension = ImageUploadRules.ResolveExtension(request.File.ContentType);
        var folder = $"providers/{providerId}/products/{product.Id}";
        var fileName = $"{Guid.NewGuid():N}{extension}";

        var url = await fileStorage.UploadAsync(
            request.File.Content,
            request.File.ContentType,
            folder,
            fileName,
            cancellationToken);

        var previous = product.ImageUrl;
        product.SetImageUrl(url);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try { await fileStorage.DeleteAsync(previous, cancellationToken); }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Best-effort blob delete failed for {Url}", previous);
        }

        logger.LogInformation(
            "Uploaded image for product {ProductId} for provider {ProviderId}",
            product.Id,
            providerId);
        return product.ToDto(currentCulture.LanguageCode);
    }
}
