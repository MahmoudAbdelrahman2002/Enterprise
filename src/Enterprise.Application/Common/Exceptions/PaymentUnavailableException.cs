using Enterprise.Application.Common.Localization;

namespace Enterprise.Application.Common.Exceptions;

public sealed class PaymentUnavailableException() : AppException(MessageKeys.Payment.Unavailable);
