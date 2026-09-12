using Enterprise.Application.Common.Models;
using Enterprise.Domain.Common;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Enums;

namespace Enterprise.Application.Features.Products;

public sealed record ProductTranslationsDto(
    LocalizedText Name,
    LocalizedText Description,
    LocalizedText Category);

public sealed record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    string Category,
    decimal Price,
    int StockQuantity,
    ProductStatus Status,
    DateTime CreatedAtUtc);

public sealed record AdminProductDto(
    Guid Id,
    string Sku,
    LocalizedText Name,
    LocalizedText? Description,
    LocalizedText Category,
    decimal Price,
    int StockQuantity,
    ProductStatus Status,
    DateTime CreatedAtUtc,
    ProductTranslationsDto? Translations = null);

public static class ProductMapping
{
    public static ProductDto ToDto(this Product product, string language)
    {
        var (name, description, category) = product.ResolveContent(language);
        return new ProductDto(
            product.Id,
            product.Sku,
            name,
            description,
            category,
            product.Price,
            product.StockQuantity,
            product.Status,
            product.CreatedAtUtc);
    }

    public static AdminProductDto ToAdminDto(this Product product)
    {
        var translations = product.ToTranslationsDto();
        return new AdminProductDto(
            product.Id,
            product.Sku,
            translations.Name,
            translations.Description,
            translations.Category,
            product.Price,
            product.StockQuantity,
            product.Status,
            product.CreatedAtUtc,
            translations);
    }

    public static void ApplyLocalizedContent(
        this Product product,
        LocalizedText name,
        LocalizedText? description,
        LocalizedText category)
    {
        product.UpsertTranslation(
            SupportedLanguages.English,
            name.En,
            description?.En,
            category.En);

        ApplyOptional(product, SupportedLanguages.Italian, name.It, description?.It, category.It, name.En, description?.En, category.En);
        ApplyOptional(product, SupportedLanguages.Arabic, name.Ar, description?.Ar, category.Ar, name.En, description?.En, category.En);
    }

    private static void ApplyOptional(
        Product product,
        string language,
        string? name,
        string? description,
        string? category,
        string englishName,
        string? englishDescription,
        string englishCategory)
    {
        if (string.IsNullOrWhiteSpace(name)
            && string.IsNullOrWhiteSpace(description)
            && string.IsNullOrWhiteSpace(category))
        {
            return;
        }

        product.UpsertTranslation(
            language,
            string.IsNullOrWhiteSpace(name) ? englishName : name,
            string.IsNullOrWhiteSpace(description) ? englishDescription : description,
            string.IsNullOrWhiteSpace(category) ? englishCategory : category);
    }

    private static ProductTranslationsDto ToTranslationsDto(this Product product)
    {
        var en = product.Translations.FirstOrDefault(t => t.LanguageCode == SupportedLanguages.English);
        var it = product.Translations.FirstOrDefault(t => t.LanguageCode == SupportedLanguages.Italian);
        var ar = product.Translations.FirstOrDefault(t => t.LanguageCode == SupportedLanguages.Arabic);

        return new ProductTranslationsDto(
            new LocalizedText(en?.Name ?? string.Empty, it?.Name, ar?.Name),
            new LocalizedText(en?.Description ?? string.Empty, it?.Description, ar?.Description),
            new LocalizedText(en?.Category ?? string.Empty, it?.Category, ar?.Category));
    }
}
