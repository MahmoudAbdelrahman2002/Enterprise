using Enterprise.Application.Common.Models;

namespace Enterprise.Application.Features.Provider.Categories.DTOs;

public class UpdateCategoryDto
{
    public Guid Id { get; init; }

    public int? DisplayOrder { get; init; } 
    public LocalizedText? Name { get; init; }
    public LocalizedText? Description { get; init; }

    public bool? IsActive { get; init; }
}
