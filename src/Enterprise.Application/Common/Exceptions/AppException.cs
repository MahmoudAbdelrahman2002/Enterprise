namespace Enterprise.Application.Common.Exceptions;

/// <summary>
/// Application exception that carries a resource key plus format args so the API
/// layer can localize the client-facing message. <see cref="Exception.Message"/> is the
/// key (useful in logs), never a translated sentence.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string errorCode, params object[] args)
        : base(errorCode)
    {
        ErrorCode = errorCode;
        Args = args;
    }

    protected AppException(string errorCode, Exception? innerException, params object[] args)
        : base(errorCode, innerException)
    {
        ErrorCode = errorCode;
        Args = args;
    }

    public string ErrorCode { get; }
    public object[] Args { get; }
}
