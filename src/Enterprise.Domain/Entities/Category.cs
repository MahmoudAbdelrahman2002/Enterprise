using Enterprise.Domain.Common;

namespace Enterprise.Domain.Entities;

public sealed class Category : BaseAuditableEntity, ISoftDelete
{
    private readonly List<CategoryTranslation> _translations = [];

    private Category()
    {
    }

    public Category(Guid providerId, int displayOrder = 0, bool isActive = true)
    {
        ProviderId = providerId;
        DisplayOrder = displayOrder;
        IsActive = isActive;
    }

    public Guid ProviderId { get;  set; }
    public Provider? Provider { get;  set; }

    public int DisplayOrder { get; set; }
    public bool IsActive { get;  set; } = true;

    public IReadOnlyCollection<CategoryTranslation> Translations => _translations;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }

    public void SetActive(bool isActive) => IsActive = isActive;

    public void UpdateDetails(int displayOrder) => DisplayOrder = displayOrder;

    public CategoryTranslation? Resolve(string language)
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

        var name = !string.IsNullOrWhiteSpace(requested?.Name) ? requested.Name : english?.Name ?? string.Empty;
        var description = !string.IsNullOrWhiteSpace(requested?.Description) ? requested.Description : english?.Description;

        return (name, description);
    }

    public void UpsertTranslation(string languageCode, string name, string? description = null)
    {
        var code = SupportedLanguages.Normalize(languageCode);
        var existing = _translations.FirstOrDefault(t => t.LanguageCode == code);
        if (existing is null)
        {
            _translations.Add(new CategoryTranslation(Id, code, name, description));
            return;
        }

        existing.Update(name, description);
    }
}
