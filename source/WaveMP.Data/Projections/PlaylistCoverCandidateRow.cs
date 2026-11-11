namespace WaveMP.Data.Projections;

/// <summary>
/// An entry of a playlist whose track may lend the playlist its cover.
/// </summary>
/// <param name="PlaylistId">Id of the playlist.</param>
/// <param name="Position">Position of the entry; sorts the entries of the playlist.</param>
/// <param name="TrackId">Id of the track.</param>
/// <param name="AlbumId">Id of the album of the track.</param>
/// <param name="HasCover">Whether the track has a cover: its own, its album's or one of another track of the album.
/// </param>
public sealed record PlaylistCoverCandidateRow(
    Guid PlaylistId,
    int Position,
    Guid TrackId,
    Guid? AlbumId,
    bool HasCover);
