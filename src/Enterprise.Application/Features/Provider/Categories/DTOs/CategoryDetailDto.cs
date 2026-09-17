namespace Enterprise.Application.Features.Provider.Categories.DTOs;

public sealed class CategoryDetailDto
{
    public Guid Id { get; init; }
    public Guid ProviderId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
    public CategoryTranslationsDto? Translations { get; init; }
}
