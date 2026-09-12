namespace Enterprise.Application.Common.Models;

/// <summary>
/// The one generic response envelope this template ships, because paging metadata (total
/// count, page count, has-next) is genuinely part of the payload for any list endpoint -
/// unlike a blanket "wrap everything" envelope, this doesn't hide the resource shape or the
/// HTTP status code.
/// </summary>
public sealed class PagedResult<T>
{
    public PagedResult(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    public IReadOnlyList<T> Items { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
