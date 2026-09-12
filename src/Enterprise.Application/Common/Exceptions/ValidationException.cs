using FluentValidation.Results;

namespace Enterprise.Application.Common.Exceptions;

/// <summary>
/// Raised exclusively by <c>ValidationBehavior</c> after aggregating every failed
/// <see cref="FluentValidation.IValidator{T}"/> for a request. Carries a
/// property-name -&gt; messages dictionary so the global exception handler can populate
/// <c>ValidationProblemDetails.Errors</c> in the exact shape ASP.NET Core's own
/// model-validation failures use - clients get one consistent 400 response shape regardless
/// of whether FluentValidation or `[ApiController]` model binding rejected the request.
/// </summary>
public sealed class ValidationException : Exception
{
    public ValidationException() : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures) : this()
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }

    public IDictionary<string, string[]> Errors { get; }
}
