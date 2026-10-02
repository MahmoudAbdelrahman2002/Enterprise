using Enterprise.Domain.Common;
namespace Enterprise.Domain.Entities;

public class Order : BaseEntity
{
    public Guid ProviderId { get; set; }
    public Provider Provider { get; set; } = null!;

    public Guid UserId { get; set; }

    public DateTime OrderDateUtc { get; set; }
    public string? StripeCheckoutSessionId { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public string? Notes { get; set; }

    public List<OrderItem> OrderItems { get; set; } = [];

    public void ChangeStatus(OrderStatus newStatus)
    {
        if (!OrderStatusTransitions.CanTransition(Status, newStatus))
        {
            throw new InvalidOperationException(
                $"Cannot change order status from {Status} to {newStatus}.");
        }

        Status = newStatus;
    }
}

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public enum OrderStatus
{
    Pending = 0,
    Accepted = 1,
    Preparing = 2,
    Ready = 3,
    Completed = 4,
    Cancelled = 5,
}

public static class OrderStatusTransitions
{
    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> Allowed = new()
    {
        [OrderStatus.Pending] = [OrderStatus.Accepted, OrderStatus.Cancelled],
        [OrderStatus.Accepted] = [OrderStatus.Preparing, OrderStatus.Cancelled],
        [OrderStatus.Preparing] = [OrderStatus.Ready, OrderStatus.Cancelled],
        [OrderStatus.Ready] = [OrderStatus.Completed, OrderStatus.Cancelled],
        [OrderStatus.Completed] = [],
        [OrderStatus.Cancelled] = [],
    };

    public static bool CanTransition(OrderStatus from, OrderStatus to) =>
        from != to && Allowed.TryGetValue(from, out var next) && next.Contains(to);
}
