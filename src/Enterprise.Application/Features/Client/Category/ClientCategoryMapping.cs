namespace Enterprise.Application.Features.Client.Category;

public static class ClientCategoryMapping
{
     public static ClientCategoryDto ClientCategoryToDto(this Enterprise.Domain.Entities.Category category, string? currentLanguage = null)
    {
        var (name, description) = category.ResolveContent(currentLanguage);

        return new ClientCategoryDto
        {
            Id = category.Id,
            Name = name,
            Description = description,
            ImageUrl = category.ImageUrl,
            DisplayOrder = category.DisplayOrder
        };
    }
}
