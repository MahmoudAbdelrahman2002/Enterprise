using Enterprise.Domain.Common;

namespace Enterprise.Domain.Entities;

public sealed class ProductTranslation
{
    private ProductTranslation()
    {
    }

    public ProductTranslation(Guid productId, string languageCode, string name, string? description = null)
    {
        ProductId = productId;
        LanguageCode = SupportedLanguages.Normalize(languageCode);
        Update(name, description);
    }

    public Guid ProductId { get; private set; }
    public string LanguageCode { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }

    public void Update(string name, string? description = null)
    {
        Name = name;
        Description = description;
    }
}
