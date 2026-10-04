using Enterprise.Application.Features.Orders.DTOs;
using Enterprise.Domain.Entities;

namespace Enterprise.Application.Features.Orders;

public static class OrderMapping
{
    public static OrderListItemDto ToListItemDto(this Order order) => new()
    {
        Id = order.Id,
        ProviderId = order.ProviderId,
        UserId = order.UserId,
        OrderDateUtc = order.OrderDateUtc,
        TotalAmount = order.TotalAmount,
        Status = order.Status,
        IsHistorical = order.IsHistorical
    };

    public static OrderDetailDto ToDetailDto(this Order order) => new()
    {
        Id = order.Id,
        ProviderId = order.ProviderId,
        UserId = order.UserId,
        OrderDateUtc = order.OrderDateUtc,
        TotalAmount = order.TotalAmount,
        Status = order.Status,
        IsHistorical = order.IsHistorical,
        Notes = order.Notes,
        StripeCheckoutSessionId = order.StripeCheckoutSessionId,
        Items = order.OrderItems
            .OrderBy(i => i.ProductName)
            .Select(i => new OrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            })
            .ToList()
    };
}
