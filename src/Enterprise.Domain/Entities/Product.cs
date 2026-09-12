using Enterprise.Domain.Common;
using Enterprise.Domain.Enums;
using Enterprise.Domain.Exceptions;

namespace Enterprise.Domain.Entities;

/// <summary>
/// Sample aggregate root used to demonstrate CQRS, generic repository + specifications,
/// caching and pagination/filtering/sorting throughout the template. Behaviour (not just
/// data) lives here: state transitions are exposed as intention-revealing methods
/// (<see cref="DecreaseStock"/>, <see cref="Discontinue"/>) rather than public setters,
/// so an invalid state (e.g. negative stock) is impossible to construct from outside.
/// Catalog copy (name, description, category) lives on <see cref="ProductTranslation"/>.
/// </summary>
public sealed class Product : BaseAuditableEntity, ISoftDelete
{
    private readonly List<ProductTranslation> _translations = [];

    private Product()
    {
    }

    public Product(string sku, decimal price, int stockQuantity)
    {
        Sku = sku;
        Price = price;
        StockQuantity = stockQuantity;
        Status = stockQuantity > 0 ? ProductStatus.Active : ProductStatus.OutOfStock;
    }

    public string Sku { get; private set; } = null!;
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    public ProductStatus Status { get; private set; }

    public IReadOnlyCollection<ProductTranslation> Translations => _translations;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }

    public ProductTranslation Resolve(string language)
    {
        var code = SupportedLanguages.Normalize(language);
        return _translations.FirstOrDefault(t => t.LanguageCode == code)
               ?? _translations.FirstOrDefault(t => t.LanguageCode == SupportedLanguages.English)
               ?? throw new InvalidOperationException("Product is missing a required English translation.");
    }

    public (string Name, string? Description, string Category) ResolveContent(string? language)
    {
        var code = SupportedLanguages.Normalize(language);
        var requested = _translations.FirstOrDefault(t => t.LanguageCode == code);
        var english = _translations.FirstOrDefault(t => t.LanguageCode == SupportedLanguages.English)
                      ?? _translations.FirstOrDefault()
                      ?? throw new InvalidOperationException("Product is missing a required English translation.");

        var name = !string.IsNullOrWhiteSpace(requested?.Name) ? requested.Name : english.Name;
        var description = !string.IsNullOrWhiteSpace(requested?.Description) ? requested.Description : english.Description;
        var category = !string.IsNullOrWhiteSpace(requested?.Category) ? requested.Category : english.Category;

        return (name, description, category);
    }

    public void UpsertTranslation(string languageCode, string name, string? description, string category)
    {
        var code = SupportedLanguages.Normalize(languageCode);
        var existing = _translations.FirstOrDefault(t => t.LanguageCode == code);
        if (existing is null)
        {
            _translations.Add(new ProductTranslation(Id, code, name, description, category));
            return;
        }

        existing.Update(name, description, category);
    }

    public void UpdateDetails(decimal price)
    {
        Price = price;
    }

    public void DecreaseStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new InvalidQuantityException();
        }

        if (quantity > StockQuantity)
        {
            throw new InsufficientStockException(Id, quantity, StockQuantity);
        }

        StockQuantity -= quantity;
        if (StockQuantity == 0)
        {
            Status = ProductStatus.OutOfStock;
        }
    }

    public void IncreaseStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new InvalidQuantityException();
        }

        StockQuantity += quantity;
        if (Status == ProductStatus.OutOfStock)
        {
            Status = ProductStatus.Active;
        }
    }

    public void Discontinue() => Status = ProductStatus.Discontinued;
}
