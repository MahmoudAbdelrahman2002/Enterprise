using Enterprise.Domain.Common;

namespace Enterprise.Domain.Entities;

public class ProductTranslation
{
    public ProductTranslation()
    {

    }
    
     public ProductTranslation(string languageCode, string name, string? description = null)
       : this(Guid.Empty, languageCode, name, description)
    {
    }
    public ProductTranslation(Guid productId, string languageCode, string name, string? description = null)
    {
        ProductId = productId;
        LanguageCode = SupportedLanguages.Normalize(languageCode);
        Name = name;
        Description = description;
    }
    public Guid ProductId { get; set; }
    public string LanguageCode { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
    }
}
