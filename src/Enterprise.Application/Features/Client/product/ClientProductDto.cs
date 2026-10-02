namespace Enterprise.Application.Features.Client.product;

public class ClientProductDto
{
    public Guid Id { get; init; }
    public Guid CategoryId { get; init; }
    public Guid ProviderId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public string? ImageUrl { get; init; }
}
