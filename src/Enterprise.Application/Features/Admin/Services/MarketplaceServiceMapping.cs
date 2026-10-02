using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Admin.Services.DTOs;
using Enterprise.Domain.Common;
using Enterprise.Domain.Entities;

namespace Enterprise.Application.Features.Admin.Services;

public static class MarketplaceServiceMapping
{
    public static MarketplaceServiceDto ToDto(this MarketplaceService service, string? currentLanguage = null)
    {
        var (name, description) = service.ResolveContent(currentLanguage);
        var translations = service.ToTranslationsDto();

        return new MarketplaceServiceDto(
            service.Id,
            service.Code,
            service.IsActive,
            service.DisplayOrder,
            name,
            description,
            translations,
            service.ImageUrl,
            service.CreatedAtUtc,
            service.LastModifiedAtUtc);
    }

    public static MarketplaceServiceLookupDto ToLookupDto(this MarketplaceService service, string? currentLanguage = null)
    {
        var (name, description) = service.ResolveContent(currentLanguage);
        return new MarketplaceServiceLookupDto(
            service.Id,
            service.Code,
            name,
            description);
    }

    public static void ApplyLocalizedContent(
        this MarketplaceService service,
        LocalizedText name,
        LocalizedText? description)
    {
        LocalizedContentHelper.Apply(service.UpsertTranslation, name, description);
    }

    private static MarketplaceServiceTranslationsDto ToTranslationsDto(this MarketplaceService service)
    {
        var en = service.Translations.FirstOrDefault(t => t.LanguageCode == SupportedLanguages.English);
        var it = service.Translations.FirstOrDefault(t => t.LanguageCode == SupportedLanguages.Italian);
        var ar = service.Translations.FirstOrDefault(t => t.LanguageCode == SupportedLanguages.Arabic);

        return new MarketplaceServiceTranslationsDto(
            new LocalizedText(en?.Name ?? service.Code, it?.Name, ar?.Name),
            new LocalizedText(en?.Description ?? string.Empty, it?.Description, ar?.Description));
    }
}
