namespace Enterprise.Domain.Exceptions;

public sealed class InvalidQuantityException()
    : DomainException("Product.QuantityMustBePositive");
