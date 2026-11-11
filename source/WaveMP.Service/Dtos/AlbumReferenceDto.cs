namespace WaveMP.Service.Dtos;

/// <summary>
/// The album of a track.
/// </summary>
public sealed class AlbumReferenceDto
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    /// <summary>
    /// Album artist; null for compilations and albums without a known artist.
    /// </summary>
    public ArtistReferenceDto? Artist { get; init; }

    /// <summary>
    /// Tracks of several artists without a common album artist. Clients show their own text for it.
    /// </summary>
    public required bool IsCompilation { get; init; }
}
