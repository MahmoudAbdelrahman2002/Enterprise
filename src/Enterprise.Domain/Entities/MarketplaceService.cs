using Enterprise.Domain.Common;

namespace Enterprise.Domain.Entities;

/// <summary>
/// Business service category in the Subito marketplace (e.g. Restaurant, Pharmacy, Grocery).
/// Managed by Administrators and assigned to Providers.
/// </summary>
public sealed class MarketplaceService : BaseAuditableEntity, ISoftDelete
{
    private readonly List<MarketplaceServiceTranslation> _translations = [];

    private MarketplaceService()
    {
    }

    public MarketplaceService(string code, int displayOrder = 0, bool isActive = true)
    {
        Code = code.Trim().ToLowerInvariant();
        DisplayOrder = displayOrder;
        IsActive = isActive;
    }

    public string Code { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;
    public int DisplayOrder { get; private set; }

    public IReadOnlyCollection<MarketplaceServiceTranslation> Translations => _translations;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }

    public MarketplaceServiceTranslation? Resolve(string language)
    {
        var code = SupportedLanguages.Normalize(language);
        return _translations.FirstOrDefault(t => t.LanguageCode == code)
               ?? _translations.FirstOrDefault(t => t.LanguageCode == SupportedLanguages.English)
               ?? _translations.FirstOrDefault();
    }

    public (string Name, string? Description) ResolveContent(string? language)
    {
        var code = SupportedLanguages.Normalize(language);
        var requested = _translations.FirstOrDefault(t => t.LanguageCode == code);
        var english = _translations.FirstOrDefault(t => t.LanguageCode == SupportedLanguages.English)
                      ?? _translations.FirstOrDefault();

        var name = !string.IsNullOrWhiteSpace(requested?.Name) ? requested.Name : english?.Name ?? Code;
        var description = !string.IsNullOrWhiteSpace(requested?.Description) ? requested.Description : english?.Description;

        return (name, description);
    }
    
    public void UpsertTranslation(string languageCode, string name, string? description = null)
    {
        var code = SupportedLanguages.Normalize(languageCode);
        var existing = _translations.FirstOrDefault(t => t.LanguageCode == code);
        if (existing is null)
        {
            _translations.Add(new MarketplaceServiceTranslation(Id, code, name, description));
            return;
        }

        existing.Update(name, description);
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    public void UpdateDetails(string code, int displayOrder)
    {
        Code = code.Trim().ToLowerInvariant();
        DisplayOrder = displayOrder;
    }
}
