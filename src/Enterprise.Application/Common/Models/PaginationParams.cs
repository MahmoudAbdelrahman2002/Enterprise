namespace Enterprise.Application.Common.Models;

/// <summary>
/// Base class for every list query's model-bound query-string parameters. Validates
/// <see cref="PageSize"/> server-side (never trust a client-supplied page size) so a caller
/// can't request PageSize=1000000 and force the database to materialize the whole table.
/// </summary>
public abstract record PaginationParams
{
    public const int MaxPageSize = 100;

    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
