using Enterprise.Domain.Common;
using Enterprise.Domain.Enums;

namespace Enterprise.Domain.Entities;

public sealed class Product : BaseAuditableEntity, ISoftDelete
{
    private readonly List<ProductTranslation> _translations = [];

    private Product()
    {
    }

    public Product(Guid categoryId, string sku, decimal price, ProductStatus status = ProductStatus.Draft)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ArgumentException("SKU is required.", nameof(sku));
        }

        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be greater than zero.");
        }

        CategoryId = categoryId;
        Sku = sku.Trim();
        Price = price;
        Status = status;
    }

    public Guid CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;

    public string Sku { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public ProductStatus Status { get; private set; }
    public string? ImageUrl { get; private set; }

    public IReadOnlyCollection<ProductTranslation> Translations => _translations;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }

    public void SetImageUrl(string? imageUrl) =>
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();

    public void UpdateDetails(string sku, decimal price)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ArgumentException("SKU is required.", nameof(sku));
        }

        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be greater than zero.");
        }

        Sku = sku.Trim();
        Price = price;
    }

    public void SetStatus(ProductStatus status) => Status = status;

    public void MoveToCategory(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Category is required.", nameof(categoryId));
        }

        CategoryId = categoryId;
    }

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
