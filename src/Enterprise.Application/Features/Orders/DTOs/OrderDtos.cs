using Enterprise.Domain.Entities;

namespace Enterprise.Application.Features.Orders.DTOs;

public sealed class OrderListItemDto
{
    public Guid Id { get; init; }
    public Guid ProviderId { get; init; }
    public Guid UserId { get; init; }
    public DateTime OrderDateUtc { get; init; }
    public decimal TotalAmount { get; init; }
    public OrderStatus Status { get; init; }
    public bool IsHistorical { get; init; }
}

public sealed class OrderItemDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal LineTotal => UnitPrice * Quantity;
}

public sealed class OrderDetailDto
{
    public Guid Id { get; init; }
    public Guid ProviderId { get; init; }
    public Guid UserId { get; init; }
    public DateTime OrderDateUtc { get; init; }
    public decimal TotalAmount { get; init; }
    public OrderStatus Status { get; init; }
    public bool IsHistorical { get; init; }
    public string? Notes { get; init; }
    public string? StripeCheckoutSessionId { get; init; }
    public IReadOnlyList<OrderItemDto> Items { get; init; } = [];
}

public sealed class UpdateOrderStatusDto
{
    public OrderStatus Status { get; init; }
}
