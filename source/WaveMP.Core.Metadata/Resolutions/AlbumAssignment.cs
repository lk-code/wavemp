namespace WaveMP.Core.Metadata.Resolutions;

/// <summary>
/// The album of a track: its title, its album artist or whether it is a compilation, and its key.
/// </summary>
/// <param name="Title">Title of the album.</param>
/// <param name="AlbumArtist">Album artist; <c>null</c> for compilations and albums without a known artist.</param>
/// <param name="IsCompilation">Tracks of several artists without a common album artist.</param>
/// <param name="Year">Year of the album, the first one its tracks name.</param>
/// <param name="Key">Identity of the album, see <see cref="AlbumKeys"/>.</param>
public sealed record AlbumAssignment(string Title, string? AlbumArtist, bool IsCompilation, int? Year, string Key);
