using Enterprise.Domain.Common;

namespace Enterprise.Domain.Entities;

/// <summary>
/// Multilingual translation for a marketplace service.
/// Supported languages are en, ar, and it.
/// </summary>
public sealed class MarketplaceServiceTranslation
{
    private MarketplaceServiceTranslation()
    {
    }

    public MarketplaceServiceTranslation(string languageCode, string name, string? description = null)
        : this(Guid.Empty, languageCode, name, description)
    {
    }

    public MarketplaceServiceTranslation(Guid marketplaceServiceId, string languageCode, string name, string? description = null)
    {
        MarketplaceServiceId = marketplaceServiceId;
        LanguageCode = SupportedLanguages.Normalize(languageCode);
        Update(name, description);
    }

    public Guid MarketplaceServiceId { get; private set; }
    public string LanguageCode { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
    }
}
