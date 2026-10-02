using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Provider.Products.DTOs;
using Enterprise.Domain.Common;
using Enterprise.Domain.Entities;

namespace Enterprise.Application.Features.Provider.Products;

public static class ProductMapping
{
    public static ProductListItemDto ToListItemDto(this Product product, string? currentLanguage = null)
    {
        var (name, description) = product.ResolveContent(currentLanguage);

        return new ProductListItemDto
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            Name = name,
            Description = description,
            Sku = product.Sku,
            Price = product.Price,
            Status = product.Status,
            ImageUrl = product.ImageUrl,
        };
    }

    public static ProductDetailDto ToDto(this Product product, string? currentLanguage = null, bool includeTranslations = true)
    {
        var (name, description) = product.ResolveContent(currentLanguage);
        if (includeTranslations)
        {
            return new ProductDetailDto
            {
                Id = product.Id,
                CategoryId = product.CategoryId,
                Name = name,
                Description = description,
                Translations = product.ToTranslationsDto(),
                Sku = product.Sku,
                Price = product.Price,
                Status = product.Status,
                ImageUrl = product.ImageUrl,
            };
        }

        return new ProductDetailDto
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            Name = name,
            Description = description,
            Sku = product.Sku,
            Price = product.Price,
            Status = product.Status,
            ImageUrl = product.ImageUrl,
        };
    }

    public static void ApplyLocalizedContent(
        this Product product,
        LocalizedText name,
        LocalizedText? description)
    {
        LocalizedContentHelper.Apply(product.UpsertTranslation, name, description);
    }

    private static ProductTranslationsDto ToTranslationsDto(this Product product)
    {
        var en = product.Translations.FirstOrDefault(t => t.LanguageCode == SupportedLanguages.English);
        var it = product.Translations.FirstOrDefault(t => t.LanguageCode == SupportedLanguages.Italian);
        var ar = product.Translations.FirstOrDefault(t => t.LanguageCode == SupportedLanguages.Arabic);

        return new ProductTranslationsDto(
            new LocalizedText(en?.Name ?? string.Empty, it?.Name, ar?.Name),
            new LocalizedText(en?.Description ?? string.Empty, it?.Description, ar?.Description));
    }
}
