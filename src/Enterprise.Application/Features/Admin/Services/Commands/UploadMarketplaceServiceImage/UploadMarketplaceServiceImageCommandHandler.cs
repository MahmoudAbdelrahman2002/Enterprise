using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Images;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Admin.Services.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Admin.Services.Commands.UploadMarketplaceServiceImage;

public sealed class UploadMarketplaceServiceImageCommandValidator : AbstractValidator<UploadMarketplaceServiceImageCommand>
{
    public UploadMarketplaceServiceImageCommandValidator(IAppLocalizer localizer)
    {
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

public sealed class UploadMarketplaceServiceImageCommandHandler(
    IUnitOfWork unitOfWork,
    IFileStorageService fileStorage,
    ICurrentCulture currentCulture,
    ILogger<UploadMarketplaceServiceImageCommandHandler> logger) : IRequestHandler<UploadMarketplaceServiceImageCommand, MarketplaceServiceDto>
{
    public async Task<MarketplaceServiceDto> Handle(
        UploadMarketplaceServiceImageCommand request,
        CancellationToken cancellationToken)
    {
        var service = await unitOfWork.Services.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(MarketplaceService), request.Id);

        var extension = ImageUploadRules.ResolveExtension(request.File.ContentType);
        var folder = $"services/{service.Id}";
        var fileName = $"{Guid.NewGuid():N}{extension}";

        var url = await fileStorage.UploadAsync(
            request.File.Content,
            request.File.ContentType,
            folder,
            fileName,
            cancellationToken);

        var previous = service.ImageUrl;
        service.SetImageUrl(url);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try { await fileStorage.DeleteAsync(previous, cancellationToken); }
        catch (Exception ex) { logger.LogWarning(ex, "Best-effort blob delete failed for {Url}", previous); }

        logger.LogInformation("Uploaded marketplace service image {ServiceId}", service.Id);
        return service.ToDto(currentCulture.LanguageCode);
    }
}
