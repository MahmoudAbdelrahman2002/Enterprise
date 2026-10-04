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
    public string? PreviousStatus { get; set; }
    public bool IsHistorical => PreviousStatus == "Cancelled";
    public string? Notes { get; set; }

    public List<OrderItem> OrderItems { get; set; } = [];

    public void ChangeStatus(OrderStatus newStatus)
    {
        if (IsHistorical || !OrderStatusTransitions.CanTransition(Status, newStatus))
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
    New = 0,
    Preparing = 2,
    Ready = 3,
}

public static class OrderStatusTransitions
{
    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> Allowed = new()
    {
        [OrderStatus.New] = [OrderStatus.Preparing],
        [OrderStatus.Preparing] = [OrderStatus.Ready],
        [OrderStatus.Ready] = [],
    };

    public static bool CanTransition(OrderStatus from, OrderStatus to) =>
        from != to && Allowed.TryGetValue(from, out var next) && next.Contains(to);
}
