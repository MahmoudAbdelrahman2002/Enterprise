using System.Linq.Expressions;
using Enterprise.Domain.Common;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Enums;

namespace Enterprise.Domain.Specifications.Services;

public sealed class ProductFilterSpecification : BaseSpecification<Product>
{
    private ProductFilterSpecification(
        Guid? categoryId,
        string? searchTerm,
        ProductStatus? status,
        string language,
        Guid? providerId = null,
        bool? categoryIsActive = null)
        : base(BuildCriteria(categoryId, searchTerm, status, language, providerId, categoryIsActive))
    {
        AddInclude(p => p.Translations);
        AddInclude(p => p.Category);
    }

    public static ProductFilterSpecification ForCount(
        Guid? categoryId, string? searchTerm, ProductStatus? status, string language) =>
        new(categoryId, searchTerm, status, language);

    public static ProductFilterSpecification ForPage(
        Guid? categoryId,
        string? searchTerm,
        ProductStatus? status,
        int pageNumber,
        int pageSize,
        string language)
    {
        var spec = new ProductFilterSpecification(categoryId, searchTerm, status, language);
        spec.ApplyOrderBy(p => p.Sku);
        spec.ApplyPagingInternal((pageNumber - 1) * pageSize, pageSize);
        return spec;
    }

    public static ProductFilterSpecification ForProviderCount(
        Guid providerId, Guid? categoryId, string? searchTerm, ProductStatus? status, string language) =>
        new(categoryId, searchTerm, status, language, providerId, categoryIsActive: true);

    public static ProductFilterSpecification ForProviderPage(
        Guid providerId,
        Guid? categoryId,
        string? searchTerm,
        ProductStatus? status,
        int pageNumber,
        int pageSize,
        string language)
    {
        var spec = new ProductFilterSpecification(
            categoryId, searchTerm, status, language, providerId, categoryIsActive: true);
        spec.ApplyOrderBy(p => p.Sku);
        spec.ApplyPagingInternal((pageNumber - 1) * pageSize, pageSize);
        return spec;
    }

    public static ProductFilterSpecification ForProviderCatalogCount(
        Guid providerId,
        Guid? categoryId,
        string? searchTerm,
        ProductStatus? status,
        string language) =>
        new(categoryId, searchTerm, status, language, providerId, categoryIsActive: null);

    public static ProductFilterSpecification ForProviderCatalogPage(
        Guid providerId,
        Guid? categoryId,
        string? searchTerm,
        ProductStatus? status,
        int pageNumber,
        int pageSize,
        string language)
    {
        var spec = new ProductFilterSpecification(
            categoryId, searchTerm, status, language, providerId, categoryIsActive: null);
        spec.ApplyOrderBy(p => p.Sku);
        spec.ApplyPagingInternal((pageNumber - 1) * pageSize, pageSize);
        return spec;
    }

    public static ProductFilterSpecification ForLookup(Guid? categoryId, string language)
    {
        var spec = new ProductFilterSpecification(categoryId, null, ProductStatus.Active, language);
        spec.ApplyOrderBy(p => p.Sku);
        return spec;
    }

    private static Expression<Func<Product, bool>> BuildCriteria(
        Guid? categoryId,
        string? searchTerm,
        ProductStatus? status,
        string language,
        Guid? providerId,
        bool? categoryIsActive)
    {
        var lang = SupportedLanguages.Normalize(language);
        var english = SupportedLanguages.English;

        return product =>
            (!categoryId.HasValue || product.CategoryId == categoryId.Value) &&
            (!providerId.HasValue || product.Category.ProviderId == providerId.Value) &&
            (!categoryIsActive.HasValue || product.Category.IsActive == categoryIsActive.Value) &&
            (string.IsNullOrWhiteSpace(searchTerm) ||
                product.Sku.Contains(searchTerm) ||
                product.Translations.Any(t => t.LanguageCode == lang && t.Name.Contains(searchTerm)) ||
                (!product.Translations.Any(t => t.LanguageCode == lang) &&
                 product.Translations.Any(t => t.LanguageCode == english && t.Name.Contains(searchTerm)))) &&
            (!status.HasValue || product.Status == status.Value);
    }

    private void ApplyPagingInternal(int skip, int take) => ApplyPaging(skip, take);
}
