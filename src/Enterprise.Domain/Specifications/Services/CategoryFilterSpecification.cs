using System.Linq.Expressions;
using Enterprise.Domain.Common;
using Enterprise.Domain.Entities;

namespace Enterprise.Domain.Specifications.Services;

public sealed class CategoryFilterSpecification : BaseSpecification<Category>
{
    private CategoryFilterSpecification(Guid? providerId, string? searchTerm, bool? isActive, string language)
        : base(BuildCriteria(providerId, searchTerm, isActive, language))
    {
        AddInclude(s => s.Translations);
    }

    public static CategoryFilterSpecification ForCount(
        Guid? providerId, string? searchTerm, bool? isActive, string language) =>
        new(providerId, searchTerm, isActive, language);

    public static CategoryFilterSpecification ForPage(
        Guid? providerId,
        string? searchTerm,
        bool? isActive,
        int pageNumber,
        int pageSize,
        string language)
    {
        var spec = new CategoryFilterSpecification(providerId, searchTerm, isActive, language);
        spec.ApplyOrderBy(s => s.DisplayOrder);
        spec.ApplyPagingInternal((pageNumber - 1) * pageSize, pageSize);
        return spec;
    }

    public static CategoryFilterSpecification ForLookup(Guid? providerId, string language)
    {
        var spec = new CategoryFilterSpecification(providerId, null, true, language);
        spec.ApplyOrderBy(s => s.DisplayOrder);
        return spec;
    }

    private static Expression<Func<Category, bool>> BuildCriteria(
        Guid? providerId, string? searchTerm, bool? isActive, string language)
    {
        var lang = SupportedLanguages.Normalize(language);
        var english = SupportedLanguages.English;

        return category =>
            (!providerId.HasValue || category.ProviderId == providerId.Value) &&
            (string.IsNullOrWhiteSpace(searchTerm) ||
                category.Translations.Any(t => t.LanguageCode == lang && t.Name.Contains(searchTerm)) ||
                (!category.Translations.Any(t => t.LanguageCode == lang) &&
                 category.Translations.Any(t => t.LanguageCode == english && t.Name.Contains(searchTerm)))) &&
            (!isActive.HasValue || category.IsActive == isActive.Value);
    }

    private void ApplyPagingInternal(int skip, int take) => ApplyPaging(skip, take);
}
