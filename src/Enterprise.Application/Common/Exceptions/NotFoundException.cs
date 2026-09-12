using Enterprise.Application.Common.Localization;

namespace Enterprise.Application.Common.Exceptions;

/// <summary>
/// Thrown by handlers when a requested aggregate does not exist. Caught by the Api layer's
/// single global exception handler and translated to HTTP 404 - handlers
/// never touch HTTP status codes directly, keeping Application free of any ASP.NET Core reference.
/// </summary>
public sealed class NotFoundException : AppException
{
    public NotFoundException(string errorCode, params object[] args)
        : base(errorCode, args)
    {
    }

    public static NotFoundException For(string entityName, object key) =>
        new(MessageKeys.Entity.NotFound, entityName, key);
}
