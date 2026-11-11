namespace WaveMP.Data.Repositories;

/// <summary>
/// Sort key of the artist list.
/// </summary>
public enum ArtistSortField
{
    Name,
    FirstSeenAt,

    /// <summary>
    /// How well the name matches the search text; only with a search text.
    /// </summary>
    Relevance,
}
