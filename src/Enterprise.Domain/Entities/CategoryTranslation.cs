using Enterprise.Domain.Common;

namespace Enterprise.Domain.Entities;

public sealed class CategoryTranslation
{
    private CategoryTranslation()
    {
    }

    public CategoryTranslation(Guid categoryId, string languageCode, string name, string? description = null)
    {
        CategoryId = categoryId;
        LanguageCode = SupportedLanguages.Normalize(languageCode);
        Update(name, description);
    }

    public Guid CategoryId { get; private set; }
    public string LanguageCode { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }

    public void Update(string name, string? description = null)
    {
        Name = name;
        Description = description;
    }
}
