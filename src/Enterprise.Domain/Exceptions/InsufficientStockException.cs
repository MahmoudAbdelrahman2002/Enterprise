namespace Enterprise.Domain.Exceptions;

public sealed class InsufficientStockException(Guid productId, int requested, int available)
    : DomainException("Product.InsufficientStock", productId, requested, available)
{
    public Guid ProductId { get; } = productId;
    public int Requested { get; } = requested;
    public int Available { get; } = available;
}
