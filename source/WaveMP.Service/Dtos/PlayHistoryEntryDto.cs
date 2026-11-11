namespace WaveMP.Service.Dtos;

/// <summary>
/// A play of a track in the play history of the current profile.
/// </summary>
public sealed class PlayHistoryEntryDto
{
    /// <summary>
    /// Id of the entry.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// When the play started.
    /// </summary>
    public required DateTimeOffset PlayedAt { get; init; }

    /// <summary>
    /// The track that was played.
    /// </summary>
    public required TrackDto Track { get; init; }
}
