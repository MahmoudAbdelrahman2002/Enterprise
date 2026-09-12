namespace Enterprise.Api.Models;

/// <summary>
/// Unified HTTP envelope for every API success and error response.
/// </summary>
public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<string> Errors { get; init; } = [];
    public T? Data { get; init; }
    public string? TraceId { get; init; }

    public static ApiResponse<T> Ok(
        T? data,
        string message = "Success",
        int statusCode = 200,
        string? traceId = null) =>
        new()
        {
            Success = true,
            StatusCode = statusCode,
            Message = message,
            Errors = [],
            Data = data,
            TraceId = traceId
        };

    public static ApiResponse<T> Fail(
        int statusCode,
        string message,
        IEnumerable<string>? errors = null,
        string? traceId = null)
    {
        var list = errors?.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct().ToList()
                   ?? [];
        if (list.Count == 0 && !string.IsNullOrWhiteSpace(message))
        {
            list.Add(message);
        }

        return new ApiResponse<T>
        {
            Success = false,
            StatusCode = statusCode,
            Message = message,
            Errors = list,
            Data = default,
            TraceId = traceId
        };
    }
}
