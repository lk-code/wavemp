using WaveMP.Service.Validators;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Records a play of a track in the play history of the current profile.
/// </summary>
public sealed class CreatePlayHistoryEntryDto
{
    /// <summary>
    /// Id of the track that was played.
    /// </summary>
    [RequiredValue]
    public required Guid TrackId { get; init; }

    /// <summary>
    /// When the play started; now if missing. It must not lie in the future or be older than seven days, so clients
    /// can report plays they could not send right away.
    /// </summary>
    public DateTimeOffset? PlayedAt { get; init; }
}
