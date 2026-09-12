using Enterprise.Application.Common.Localization;

namespace Enterprise.Application.Common.Exceptions;

/// <summary>
/// Raised when an outbound email (OTP, etc.) cannot be delivered via SMTP.
/// </summary>
public sealed class EmailDeliveryException : AppException
{
    public EmailDeliveryException(Exception? innerException = null)
        : base(MessageKeys.Error.EmailDelivery, innerException)
    {
    }
}
