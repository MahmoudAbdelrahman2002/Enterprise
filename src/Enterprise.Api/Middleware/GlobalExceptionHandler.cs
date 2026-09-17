using Enterprise.Api.Models;
using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace Enterprise.Api.Middleware;

/// <summary>
/// Turns exceptions into the unified <see cref="ApiResponse{T}"/> envelope for API clients.
/// Never returns stack traces, file paths, or type names to the frontend.
/// </summary>
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IAppLocalizer localizer)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, message, errors) = MapException(exception);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(exception, "Handled {ExceptionType} for {Method} {Path}: {Message}",
                exception.GetType().Name,
                httpContext.Request.Method,
                httpContext.Request.Path,
                exception.Message);
        }

        httpContext.Response.StatusCode = statusCode;

        var body = ApiResponse<object?>.Fail(
            statusCode,
            message,
            errors,
            httpContext.TraceIdentifier);

        await httpContext.Response.WriteAsJsonAsync(body, cancellationToken);
        return true;
    }

    private (int StatusCode, string Message, IReadOnlyList<string> Errors) MapException(Exception exception) =>
        exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                localizer[MessageKeys.Validation.Failed],
                FlattenValidationErrors(validationException)),
            NotFoundException ex => Fail(StatusCodes.Status404NotFound, ex, MessageKeys.Error.NotFound),
            ConflictException ex => Fail(StatusCodes.Status409Conflict, ex, MessageKeys.Error.Conflict),
            AuthenticationFailedException ex => Fail(StatusCodes.Status401Unauthorized, ex, MessageKeys.Error.Unauthorized),
            ForbiddenAccessException => (
                StatusCodes.Status403Forbidden,
                localizer[MessageKeys.Error.Forbidden],
                [localizer[MessageKeys.Error.Forbidden]]),
            DomainException ex => (
                StatusCodes.Status409Conflict,
                ResolveMessage(ex.ErrorCode, ex.Args, MessageKeys.Error.BusinessRule),
                [ResolveMessage(ex.ErrorCode, ex.Args, MessageKeys.Error.BusinessRule)]),
            EmailDeliveryException ex => Fail(StatusCodes.Status503ServiceUnavailable, ex, MessageKeys.Error.EmailDelivery),
            AppException ex => Fail(StatusCodes.Status400BadRequest, ex, MessageKeys.Error.Unexpected),
            _ => (
                StatusCodes.Status500InternalServerError,
                localizer[MessageKeys.Error.Unexpected],
                [localizer[MessageKeys.Error.Unexpected]])
        };

    private (int StatusCode, string Message, IReadOnlyList<string> Errors) Fail(
        int statusCode, AppException exception, string fallbackKey)
    {
        var message = ResolveMessage(exception.ErrorCode, exception.Args, fallbackKey);

        var errors = exception.Errors.Count > 0
            ? exception.Errors.Select(e => ResolveMessage(e, [], fallbackKey)).Where(e => !string.IsNullOrWhiteSpace(e)).Distinct().ToList()
            : [message];

        // If there are specific errors and message was a fallback or a semicolon-joined string, pick the first error as message
        if (errors.Count > 0 && (message == localizer[fallbackKey] || (!string.IsNullOrWhiteSpace(exception.ErrorCode) && exception.ErrorCode.Contains(';'))))
        {
            message = errors[0];
        }

        return (statusCode, message, errors);
    }

    private string ResolveMessage(string? errorCode, object[] args, string fallbackKey)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            return localizer[fallbackKey];
        }

        var localized = args.Length == 0
            ? localizer[errorCode]
            : localizer[errorCode, args];

        if (!string.IsNullOrWhiteSpace(localized) && localized != errorCode)
        {
            return localized;
        }

        // If errorCode contains spaces or doesn't have a dot (resource key notation), it is already a descriptive message
        if (errorCode.Contains(' ') || !errorCode.Contains('.'))
        {
            return errorCode;
        }

        return localizer[fallbackKey];
    }

    private IReadOnlyList<string> FlattenValidationErrors(ValidationException exception)
    {
        var flattened = exception.Errors
            .SelectMany(pair => pair.Value.Select(message =>
            {
                var resolvedMessage = ResolveValidationMessage(message);
                return $"{pair.Key}: {resolvedMessage}";
            }))
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Distinct()
            .ToList();

        return flattened.Count > 0
            ? flattened
            : [localizer[MessageKeys.Validation.CheckRequest]];
    }

    private string ResolveValidationMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        var localized = localizer[message];
        if (!string.IsNullOrWhiteSpace(localized) && localized != message)
        {
            return localized;
        }

        return message;
    }
}
