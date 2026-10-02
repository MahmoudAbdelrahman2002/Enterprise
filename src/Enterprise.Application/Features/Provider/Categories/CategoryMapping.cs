using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Provider.Categories.DTOs;
using Enterprise.Domain.Common;
using Enterprise.Domain.Entities;

namespace Enterprise.Application.Features.Provider.Categories;

public static class CategoryMapping
{
    public static CategoryDetailDto ToDto(this Category category, string? currentLanguage = null, bool includeTranslations = true)
    {
        var (name, description) = category.ResolveContent(currentLanguage);
        if (includeTranslations)
        {
            var translations = category.ToTranslationsDto();

            return new CategoryDetailDto
            {
                Id = category.Id,
                ProviderId = category.ProviderId,
                Name = name,
                Description = description,
                DisplayOrder = category.DisplayOrder,
                IsActive = category.IsActive,
                ImageUrl = category.ImageUrl,
                Translations = translations,
            };
        }

        return new CategoryDetailDto
        {
            Id = category.Id,
            ProviderId = category.ProviderId,
            Name = name,
            Description = description,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive,
            ImageUrl = category.ImageUrl,
        };
    }

    public static CategoryLookupDto ToLookupDto(this Category category, string? currentLanguage = null)
    {
        var (name, _) = category.ResolveContent(currentLanguage);
        return new CategoryLookupDto(category.Id, name, category.DisplayOrder);
    }

    public static void ApplyLocalizedContent(
        this Category category,
        LocalizedText name,
        LocalizedText? description)
    {
        LocalizedContentHelper.Apply(category.UpsertTranslation, name, description);
    }

    private static CategoryTranslationsDto ToTranslationsDto(this Category category)
    {
        var en = category.Translations.FirstOrDefault(t => t.LanguageCode == SupportedLanguages.English);
        var it = category.Translations.FirstOrDefault(t => t.LanguageCode == SupportedLanguages.Italian);
        var ar = category.Translations.FirstOrDefault(t => t.LanguageCode == SupportedLanguages.Arabic);

        return new CategoryTranslationsDto(
            new LocalizedText(en?.Name ?? string.Empty, it?.Name, ar?.Name),
            new LocalizedText(en?.Description ?? string.Empty, it?.Description, ar?.Description));
    }
}
