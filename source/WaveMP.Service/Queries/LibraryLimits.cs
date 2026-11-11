using WaveMP.Core.Library.Searches;

namespace WaveMP.Service.Queries;

/// <summary>
/// Limits of the query parameters of the library.
/// </summary>
public static class LibraryLimits
{
    /// <summary>
    /// Longest search text (<c>q</c>).
    /// </summary>
    public const int TextMaxLength = SearchText.MaxLength;

    /// <summary>
    /// Longest genre of the filter, as long as a stored genre.
    /// </summary>
    public const int GenreMaxLength = 128;

    public const int MinYear = 1;

    public const int MaxYear = 9999;
}
