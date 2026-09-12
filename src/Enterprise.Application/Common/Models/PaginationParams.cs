namespace Enterprise.Application.Common.Models;

/// <summary>
/// Base class for every list query's model-bound query-string parameters. Caps
/// <see cref="PageSize"/> server-side (never trust a client-supplied page size) so a caller
/// can't request PageSize=1000000 and force the database to materialize the whole table.
/// </summary>
public abstract record PaginationParams
{
    public const int MaxPageSize = 100;

    private int _pageSize = 20;

    public int PageNumber { get; init; } = 1;

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value switch
        {
            <= 0 => 20,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }
}
