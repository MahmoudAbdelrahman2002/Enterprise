using Enterprise.Domain.Common;

namespace Enterprise.Domain.Entities;

public class ShoppingCartItem 
{
    public Guid Id { get; set; }
    public Guid ShoppingCartId { get; set; }
    public ShoppingCart ShoppingCart { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
}
