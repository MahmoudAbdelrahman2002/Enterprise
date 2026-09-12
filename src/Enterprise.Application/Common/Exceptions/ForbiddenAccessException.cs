using Enterprise.Application.Common.Localization;

namespace Enterprise.Application.Common.Exceptions;

/// <summary>Maps to HTTP 403 - the caller is authenticated but not allowed to perform this
/// specific action (distinct from 401, which means "who are you?").</summary>
public sealed class ForbiddenAccessException() : AppException(MessageKeys.Error.Forbidden);
