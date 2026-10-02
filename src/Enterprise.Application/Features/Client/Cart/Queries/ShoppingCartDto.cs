namespace Enterprise.Application.Features.Client.Cart.Queries;

public class ShoppingCartDto
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public List<ShoppingCartItemDto> Items { get; set; } = [];
    public decimal TotalPrice { get; set; }
}
