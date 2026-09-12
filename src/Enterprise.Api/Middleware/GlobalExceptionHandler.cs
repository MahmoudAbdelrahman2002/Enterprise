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
                Localize(ex.ErrorCode, ex.Args, MessageKeys.Error.BusinessRule),
                [Localize(ex.ErrorCode, ex.Args, MessageKeys.Error.BusinessRule)]),
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
        var message = Localize(exception.ErrorCode, exception.Args, fallbackKey);
        return (statusCode, message, [message]);
    }

    private string Localize(string errorCode, object[] args, string fallbackKey)
    {
        var localized = args.Length == 0
            ? localizer[errorCode]
            : localizer[errorCode, args];

        if (string.IsNullOrWhiteSpace(localized) || localized == errorCode)
        {
            return localizer[fallbackKey];
        }

        return localized;
    }

    private IReadOnlyList<string> FlattenValidationErrors(ValidationException exception)
    {
        var flattened = exception.Errors
            .SelectMany(pair => pair.Value.Select(message => $"{pair.Key}: {message}"))
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Distinct()
            .ToList();

        return flattened.Count > 0
            ? flattened
            : [localizer[MessageKeys.Validation.CheckRequest]];
    }
}
