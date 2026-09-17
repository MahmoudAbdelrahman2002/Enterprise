namespace Enterprise.Application.Common.Exceptions;

/// <summary>
/// Application exception that carries a resource key or detailed error message plus format args
/// and optional list of sub-errors so the API layer can construct a detailed client-facing response.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string errorCode, params object[] args)
        : this(errorCode, (IEnumerable<string>?)null, args)
    {
    }

    protected AppException(string errorCode, IEnumerable<string>? errors, params object[] args)
        : base(errorCode)
    {
        ErrorCode = errorCode;
        Args = args;

        var list = errors?.Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
        if (list == null || list.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(errorCode) && errorCode.Contains(';'))
            {
                list = errorCode.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            }
            else
            {
                list = [];
            }
        }

        Errors = list;
    }

    protected AppException(string errorCode, Exception? innerException, params object[] args)
        : base(errorCode, innerException)
    {
        ErrorCode = errorCode;
        Args = args;
        Errors = [];
    }

    public string ErrorCode { get; }
    public object[] Args { get; }
    public IReadOnlyList<string> Errors { get; }
}
