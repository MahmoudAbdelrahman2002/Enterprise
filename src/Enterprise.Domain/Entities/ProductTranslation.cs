using Enterprise.Domain.Common;

namespace Enterprise.Domain.Entities;

/// <summary>
/// One language variant of a product's catalog copy. English is required on every product;
/// Italian and Arabic are optional and fall back to English at read time.
/// </summary>
public sealed class ProductTranslation
{
    private ProductTranslation()
    {
    }

    public ProductTranslation(string languageCode, string name, string? description, string category)
        : this(Guid.Empty, languageCode, name, description, category)
    {
    }

    public ProductTranslation(Guid productId, string languageCode, string name, string? description, string category)
    {
        ProductId = productId;
        LanguageCode = SupportedLanguages.Normalize(languageCode);
        Update(name, description, category);
    }

    public Guid ProductId { get; private set; }
    public string LanguageCode { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string Category { get; private set; } = null!;

    public void Update(string name, string? description, string category)
    {
        Name = name;
        Description = description;
        Category = category;
    }
}
