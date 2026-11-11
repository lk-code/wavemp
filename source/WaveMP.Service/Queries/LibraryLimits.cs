using WaveMP.Core.Library.Searches;
using WaveMP.Core.Metadata.Texts;

namespace WaveMP.Service.Queries;

/// <summary>
/// Limits of the query parameters and the metadata of the library.
/// </summary>
public static class LibraryLimits
{
    /// <summary>
    /// Longest search text (<c>q</c>).
    /// </summary>
    public const int TextMaxLength = SearchText.MaxLength;

    /// <summary>
    /// Longest title or name (track, artist, album), as long as a stored one.
    /// </summary>
    public const int NameMaxLength = MetadataText.MaxLength;

    /// <summary>
    /// Longest genre of the filter, as long as a stored genre.
    /// </summary>
    public const int GenreMaxLength = 128;

    public const int MinYear = 1;

    public const int MaxYear = 9999;

    /// <summary>
    /// Highest track or disc number.
    /// </summary>
    public const int MaxNumber = 9999;
}
