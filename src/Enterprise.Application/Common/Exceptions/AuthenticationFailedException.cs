using Enterprise.Application.Common.Localization;

namespace Enterprise.Application.Common.Exceptions;

/// <summary>
/// Maps to HTTP 401. Used for every login/token failure - wrong password, unknown email,
/// locked-out account, expired/revoked refresh token - and deliberately carries the SAME
/// generic message for the "unknown email" and "wrong password" cases.
/// </summary>
public sealed class AuthenticationFailedException : AppException
{
    public AuthenticationFailedException()
        : base(MessageKeys.Auth.InvalidCredentials)
    {
    }

    public AuthenticationFailedException(string errorCode, params object[] args)
        : base(errorCode, args)
    {
    }

    public AuthenticationFailedException(string errorCode, Exception innerException, params object[] args)
        : base(errorCode, innerException, args)
    {
    }
}
