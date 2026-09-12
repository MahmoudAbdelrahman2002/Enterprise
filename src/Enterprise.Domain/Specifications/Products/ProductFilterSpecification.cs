using Enterprise.Domain.Common;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Enums;

namespace Enterprise.Domain.Specifications.Products;

/// <summary>
/// Encapsulates the "list products" query shape: free-text search (SKU/localized name), optional
/// category/status filters, sorting and paging. One spec class, reused for both the paged
/// data query (<see cref="ForPage"/>) and the matching count query
/// (<see cref="ForCount"/>) - the count intentionally omits ordering/paging since neither
/// affects a COUNT(*).
/// </summary>
public sealed class ProductFilterSpecification : BaseSpecification<Product>
{
    private ProductFilterSpecification(
        string? searchTerm, string? category, ProductStatus? status, string language)
        : base(BuildCriteria(searchTerm, category, status, language))
    {
        AddInclude(p => p.Translations);
    }

    public static ProductFilterSpecification ForCount(
        string? searchTerm, string? category, ProductStatus? status, string language) =>
        new(searchTerm, category, status, language);

    public static ProductFilterSpecification ForPage(
        string? searchTerm, string? category, ProductStatus? status,
        string? sortBy, bool sortDescending, int pageNumber, int pageSize, string language)
    {
        var spec = new ProductFilterSpecification(searchTerm, category, status, language);
        spec.ApplySort(sortBy, sortDescending, language);
        spec.ApplyPagingInternal((pageNumber - 1) * pageSize, pageSize);
        return spec;
    }

    private static System.Linq.Expressions.Expression<Func<Product, bool>> BuildCriteria(
        string? searchTerm, string? category, ProductStatus? status, string language)
    {
        var lang = SupportedLanguages.Normalize(language);
        var english = SupportedLanguages.English;

        return product =>
            (string.IsNullOrWhiteSpace(searchTerm) ||
                product.Sku.Contains(searchTerm) ||
                product.Translations.Any(t => t.LanguageCode == lang && t.Name.Contains(searchTerm)) ||
                (!product.Translations.Any(t => t.LanguageCode == lang) &&
                 product.Translations.Any(t => t.LanguageCode == english && t.Name.Contains(searchTerm)))) &&
            (string.IsNullOrWhiteSpace(category) ||
                product.Translations.Any(t => t.LanguageCode == lang && t.Category == category) ||
                (!product.Translations.Any(t => t.LanguageCode == lang) &&
                 product.Translations.Any(t => t.LanguageCode == english && t.Category == category))) &&
            (!status.HasValue || product.Status == status);
    }

    private void ApplySort(string? sortBy, bool sortDescending, string language)
    {
        var lang = SupportedLanguages.Normalize(language);
        var english = SupportedLanguages.English;

        System.Linq.Expressions.Expression<Func<Product, object>> expression = sortBy?.ToLowerInvariant() switch
        {
            "price" => p => p.Price,
            "stock" or "stockquantity" => p => p.StockQuantity,
            "name" => p => p.Translations
                .Where(t => t.LanguageCode == lang)
                .Select(t => t.Name)
                .FirstOrDefault()
                ?? p.Translations
                    .Where(t => t.LanguageCode == english)
                    .Select(t => t.Name)
                    .FirstOrDefault()
                ?? string.Empty,
            _ => p => p.CreatedAtUtc
        };

        if (sortDescending)
        {
            ApplyOrderByDescending(expression);
        }
        else
        {
            ApplyOrderBy(expression);
        }
    }

    private void ApplyPagingInternal(int skip, int take) => ApplyPaging(skip, take);
}
