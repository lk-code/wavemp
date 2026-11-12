namespace WaveMP.Service.Dtos;

/// <summary>
/// A playlist of the current profile.
/// </summary>
public sealed class PlaylistDto
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    /// <summary>
    /// Number of entries; a track can be in the playlist more than once.
    /// </summary>
    public required int TrackCount { get; init; }

    /// <summary>
    /// Total duration of all entries in milliseconds.
    /// </summary>
    public required long DurationMilliseconds { get; init; }

    /// <summary>
    /// What the client shows as cover of the playlist.
    /// </summary>
    public required PlaylistCoverDto Cover { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When the name, the entries or the cover changed last.
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; init; }
}
