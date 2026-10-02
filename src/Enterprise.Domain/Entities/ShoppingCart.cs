using Enterprise.Domain.Common;

namespace Enterprise.Domain.Entities;

public class ShoppingCart : BaseAuditableEntity
{
    public Guid ProviderId { get; set; }
    public Provider Provider { get; set; } = null!;
    public Guid UserId { get; set; }
    public List<ShoppingCartItem> Items { get; set; } = [];
}
