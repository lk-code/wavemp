namespace WaveMP.Service.Dtos;

/// <summary>
/// The artists, albums and tracks that match a search text, each sorted by relevance (best first).
/// </summary>
public sealed class SearchResultDto
{
    public required PageDto<ArtistDto> Artists { get; init; }

    public required PageDto<AlbumDto> Albums { get; init; }

    public required PageDto<TrackDto> Tracks { get; init; }
}
