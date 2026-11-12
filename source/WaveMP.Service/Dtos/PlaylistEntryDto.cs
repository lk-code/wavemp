namespace WaveMP.Service.Dtos;

/// <summary>
/// An entry of a playlist: a track at a position. The same track can have several entries.
/// </summary>
public sealed class PlaylistEntryDto
{
    /// <summary>
    /// Id of the entry; it stays the same when the entry moves.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Position of the entry in the playlist, starting at 0.
    /// </summary>
    public required int Position { get; init; }

    public required DateTimeOffset AddedAt { get; init; }

    public required TrackDto Track { get; init; }
}
