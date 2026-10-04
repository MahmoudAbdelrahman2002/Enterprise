using Enterprise.Application.Common.Models;
using Enterprise.Domain.Enums;

namespace Enterprise.Application.Features.Provider.Products.DTOs;

public sealed class ProductListItemDto
{
    public Guid Id { get; init; }
    public Guid CategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Sku { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public ProductStatus Status { get; init; }
    public string? ImageUrl { get; init; }
}

public sealed class ProductDetailDto
{
    public Guid Id { get; init; }
    public Guid CategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public ProductTranslationsDto? Translations { get; init; }
    public string Sku { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public ProductStatus Status { get; init; }
    public string? ImageUrl { get; init; }
}

public sealed class CreateProductDto
{
    public LocalizedText Name { get; init; } = null!;
    public LocalizedText? Description { get; init; }
    public string Sku { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public ProductStatus Status { get; init; } = ProductStatus.Draft;
}

public sealed class UpdateProductDto
{
    public Guid? CategoryId { get; init; }
    public LocalizedText Name { get; init; } = null!;
    public LocalizedText? Description { get; init; }
    public string Sku { get; init; } = string.Empty;
    public decimal Price { get; init; }
    [System.Text.Json.Serialization.JsonRequired]
    public ProductStatus Status { get; init; }
}
