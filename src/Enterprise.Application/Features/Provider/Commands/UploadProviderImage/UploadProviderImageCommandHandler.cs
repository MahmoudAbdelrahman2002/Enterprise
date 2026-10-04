using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Images;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Providers;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Provider.Commands.UploadProviderImage;

public sealed class UploadProviderImageCommandValidator : AbstractValidator<UploadProviderImageCommand>
{
    public UploadProviderImageCommandValidator(IAppLocalizer localizer)
    {
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

public sealed class UploadProviderImageCommandHandler(
    IUnitOfWork unitOfWork,
    IProviderContext providerContext,
    IFileStorageService fileStorage,
    IProviderAdminQueryService providerAdminQuery,
    ILogger<UploadProviderImageCommandHandler> logger) : IRequestHandler<UploadProviderImageCommand, ProviderDto>
{
    public async Task<ProviderDto> Handle(UploadProviderImageCommand request, CancellationToken cancellationToken)
    {
        var providerId = await providerContext.GetProviderIdAsync(cancellationToken);
        var provider = await unitOfWork.Providers.GetByIdAsync(providerId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Provider), providerId);

        var extension = ImageUploadRules.ResolveExtension(request.File.ContentType);
        var folder = $"providers/{provider.Id}/logo";
        var fileName = $"{Guid.NewGuid():N}{extension}";

        var url = await fileStorage.UploadAsync(
            request.File.Content,
            request.File.ContentType,
            folder,
            fileName,
            cancellationToken);

        var previous = provider.ImageUrl;
        provider.SetImageUrl(url);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try { await fileStorage.DeleteAsync(previous, cancellationToken); }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Best-effort blob delete failed for {Url}", previous);
        }

        var detail = await providerAdminQuery.GetByIdAsync(provider.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(Provider), provider.Id);

        logger.LogInformation("Uploaded provider image for provider {ProviderId}", providerId);
        return detail.ToDto();
    }
}
