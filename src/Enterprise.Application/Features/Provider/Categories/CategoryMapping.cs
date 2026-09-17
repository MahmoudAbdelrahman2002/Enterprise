using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Provider.Categories.DTOs;
using Enterprise.Domain.Common;
using Enterprise.Domain.Entities;

namespace Enterprise.Application.Features.Provider.Categories;

public static class CategoryMapping
{
    public static CategoryDetailDto ToDto(this Category category, string? currentLanguage = null,bool includeTranslations = true)
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
        };

    }

    public static void ApplyLocalizedContent(
        this Category category,
        LocalizedText name,
        LocalizedText? description)
    {
        category.UpsertTranslation(
            SupportedLanguages.English,
            name.En,
            description?.En);
        //TODO: implement this in better way
        ApplyOptional(category, SupportedLanguages.Italian, name.It, description?.It, name.En, description?.En);
        ApplyOptional(category, SupportedLanguages.Arabic, name.Ar, description?.Ar, name.En, description?.En);
    }

    private static void ApplyOptional(
        Category category,
        string language,
        string? name,
        string? description,
        string englishName,
        string? englishDescription)
    {
        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(description))
        {
            return;
        }

        category.UpsertTranslation(
            language,
            string.IsNullOrWhiteSpace(name) ? englishName : name,
            string.IsNullOrWhiteSpace(description) ? englishDescription : description);
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
