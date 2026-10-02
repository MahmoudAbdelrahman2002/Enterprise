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

namespace Enterprise.Application.Features.Providers.Commands.UploadAdminProviderImage;

public sealed class UploadAdminProviderImageCommandValidator : AbstractValidator<UploadAdminProviderImageCommand>
{
    public UploadAdminProviderImageCommandValidator(IAppLocalizer localizer)
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

public sealed class UploadAdminProviderImageCommandHandler(
    IUnitOfWork unitOfWork,
    IFileStorageService fileStorage,
    IProviderAdminQueryService providerAdminQuery,
    ILogger<UploadAdminProviderImageCommandHandler> logger) : IRequestHandler<UploadAdminProviderImageCommand, ProviderDto>
{
    public async Task<ProviderDto> Handle(UploadAdminProviderImageCommand request, CancellationToken cancellationToken)
    {
        var provider = await unitOfWork.Providers.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(Provider), request.Id);

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
        catch (Exception ex) { logger.LogWarning(ex, "Best-effort blob delete failed for {Url}", previous); }

        var detail = await providerAdminQuery.GetByIdAsync(provider.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(Provider), provider.Id);

        logger.LogInformation("Uploaded admin provider image {ProviderId}", provider.Id);
        return detail.ToDto();
    }
}
