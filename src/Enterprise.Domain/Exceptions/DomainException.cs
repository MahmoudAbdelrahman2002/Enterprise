namespace Enterprise.Domain.Exceptions;

/// <summary>
/// Base type for violations of a domain invariant (a rule the entity itself must
/// enforce, e.g. "stock cannot go negative"). Deliberately distinct from the
/// Application-layer exceptions (NotFound/Validation/Conflict/Forbidden): those
/// represent use-case/orchestration failures, this represents a broken business rule
/// raised from inside an entity's own behaviour methods.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string errorCode, params object[] args)
        : base(errorCode)
    {
        ErrorCode = errorCode;
        Args = args;
    }

    public string ErrorCode { get; }
    public object[] Args { get; }
}
