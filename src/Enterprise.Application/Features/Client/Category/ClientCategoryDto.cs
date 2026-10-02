namespace Enterprise.Application.Features.Client.Category;

public class ClientCategoryDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; }
}
