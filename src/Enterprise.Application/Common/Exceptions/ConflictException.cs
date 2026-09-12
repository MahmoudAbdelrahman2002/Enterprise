namespace Enterprise.Application.Common.Exceptions;

/// <summary>Maps to HTTP 409 - the request is well-formed but conflicts with current state
/// (duplicate SKU/email, stale concurrency token, insufficient stock, etc.).</summary>
public sealed class ConflictException : AppException
{
    public ConflictException(string errorCode, params object[] args)
        : base(errorCode, args)
    {
    }
}
