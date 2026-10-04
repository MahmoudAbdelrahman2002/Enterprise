using System.Linq.Expressions;
using Enterprise.Domain.Common;
using Enterprise.Domain.Entities;

namespace Enterprise.Domain.Specifications.Services;

public sealed class MarketplaceServiceFilterSpecification : BaseSpecification<MarketplaceService>
{
    private MarketplaceServiceFilterSpecification(string? searchTerm, bool? isActive, string language, IReadOnlyList<Guid>? visibleServiceIds = null)
        : base(BuildCriteria(searchTerm, isActive, language, visibleServiceIds))
    {
        AddInclude(s => s.Translations);
    }

    public static MarketplaceServiceFilterSpecification ForCount(
        string? searchTerm, bool? isActive, string language, IReadOnlyList<Guid>? visibleServiceIds = null) =>
        new(searchTerm, isActive, language, visibleServiceIds);

    public static MarketplaceServiceFilterSpecification ForPage(
        string? searchTerm, bool? isActive, int pageNumber, int pageSize, string language, IReadOnlyList<Guid>? visibleServiceIds = null)
    {
        var spec = new MarketplaceServiceFilterSpecification(searchTerm, isActive, language, visibleServiceIds);
        spec.ApplyOrderBy(s => s.DisplayOrder);
        spec.ApplyPagingInternal((pageNumber - 1) * pageSize, pageSize);
        return spec;
    }

    public static MarketplaceServiceFilterSpecification ForLookup(string language)
    {
        var spec = new MarketplaceServiceFilterSpecification(null, true, language);
        spec.ApplyOrderBy(s => s.DisplayOrder);
        return spec;
    }

    private static Expression<Func<MarketplaceService, bool>> BuildCriteria(
        string? searchTerm, bool? isActive, string language, IReadOnlyList<Guid>? visibleServiceIds)
    {
        var lang = SupportedLanguages.Normalize(language);
        var english = SupportedLanguages.English;

        return service =>
            (string.IsNullOrWhiteSpace(searchTerm) ||
                service.Code.Contains(searchTerm) ||
                service.Translations.Any(t => t.LanguageCode == lang && t.Name.Contains(searchTerm)) ||
                (!service.Translations.Any(t => t.LanguageCode == lang) &&
                 service.Translations.Any(t => t.LanguageCode == english && t.Name.Contains(searchTerm)))) &&
            (!isActive.HasValue || service.IsActive == isActive.Value) &&
            (visibleServiceIds == null || visibleServiceIds.Contains(service.Id));
    }

    private void ApplyPagingInternal(int skip, int take) => ApplyPaging(skip, take);
}
