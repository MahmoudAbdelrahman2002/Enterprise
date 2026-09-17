using Enterprise.Domain.Common;
namespace Enterprise.Domain.Entities;

public class Product : BaseAuditableEntity, ISoftDelete
{
    private readonly List<ProductTranslation> _translations = [];
    public Guid CategoryId { get; set; }
    public Guid ProviderId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Status { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }


    public IReadOnlyCollection<ProductTranslation> Translations => _translations;

    public ProductTranslation? Resolve(string language)
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
            _translations.Add(new ProductTranslation(Id, code, name, description));
            return;
        }

        existing.Update(name, description);
    }



}
