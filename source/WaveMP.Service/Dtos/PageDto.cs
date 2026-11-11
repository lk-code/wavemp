namespace WaveMP.Service.Dtos;

/// <summary>
/// One page of a collection.
/// </summary>
public sealed class PageDto<TItem>
{
    /// <summary>
    /// Items of the page, at most limit.
    /// </summary>
    public required IReadOnlyList<TItem> Items { get; init; }

    /// <summary>
    /// Number of items skipped before this page.
    /// </summary>
    public required int Offset { get; init; }

    /// <summary>
    /// Maximum number of items of the page, as requested.
    /// </summary>
    public required int Limit { get; init; }

    /// <summary>
    /// Number of items of the whole collection.
    /// </summary>
    public required int Total { get; init; }
}
