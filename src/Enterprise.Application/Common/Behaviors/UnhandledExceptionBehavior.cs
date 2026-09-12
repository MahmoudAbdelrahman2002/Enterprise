using Enterprise.Application.Common.Exceptions;
using Enterprise.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using ValidationException = Enterprise.Application.Common.Exceptions.ValidationException;

namespace Enterprise.Application.Common.Behaviors;

/// <summary>
/// Logs at Error level only for genuinely unexpected failures (bugs, infrastructure outages).
/// The well-known Application/Domain exceptions are expected control flow (a 404, a validation
/// failure, a business-rule violation) and are logged at an appropriate lower level by the
/// Api layer's global exception handler instead - flooding the error log with every 404 a
/// client triggers would drown out the signal that actually needs paging someone at 3am.
/// </summary>
public sealed class UnhandledExceptionBehavior<TRequest, TResponse>(
    ILogger<UnhandledExceptionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly Type[] ExpectedExceptionTypes =
    [
        typeof(NotFoundException),
        typeof(ValidationException),
        typeof(ConflictException),
        typeof(ForbiddenAccessException),
        typeof(AuthenticationFailedException),
        typeof(DomainException),
        typeof(EmailDeliveryException)
    ];

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (Exception ex) when (!IsExpected(ex))
        {
            logger.LogError(ex, "Unhandled exception for request {RequestName} {@Request}",
                typeof(TRequest).Name, request);
            throw;
        }
    }

    private static bool IsExpected(Exception ex) =>
        ExpectedExceptionTypes.Any(expected => expected.IsInstanceOfType(ex));
}
