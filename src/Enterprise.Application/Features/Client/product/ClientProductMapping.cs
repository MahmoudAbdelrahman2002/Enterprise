namespace Enterprise.Application.Features.Client.product;

public static class ClientProductMapping
{
    public static ClientProductDto ProductToDto(this Enterprise.Domain.Entities.Product product, string? currentLanguage = null)
    {
        var (name, description) = product.ResolveContent(currentLanguage);

        return new ClientProductDto
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            ProviderId = product.Category?.ProviderId ?? Guid.Empty,
            Name = name,
            Description = description,
            Price = product.Price,
            ImageUrl = product.ImageUrl,
        };
    }
}
