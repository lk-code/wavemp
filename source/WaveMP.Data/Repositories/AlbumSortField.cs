namespace WaveMP.Data.Repositories;

/// <summary>
/// Sort key of the album list.
/// </summary>
public enum AlbumSortField
{
    Title,

    /// <summary>
    /// Album artist, then title. Compilations and albums without artist come last.
    /// </summary>
    Artist,

    Year,
    FirstSeenAt,

    /// <summary>
    /// How well title and album artist match the search text; only with a search text.
    /// </summary>
    Relevance,
}
