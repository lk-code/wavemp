using WaveMP.Data.Projections;

namespace WaveMP.Data.Repositories;

/// <summary>
/// Result of a change of the entries of a playlist.
/// </summary>
/// <param name="Status">Whether the change was made.</param>
/// <param name="Entries">The new or moved entries with their positions after the change; empty otherwise.</param>
/// <param name="Max">The largest allowed position for <see cref="PlaylistEntryChangeStatus.InvalidPosition"/>, the
/// largest number of entries for <see cref="PlaylistEntryChangeStatus.Full"/>.</param>
public sealed record PlaylistEntryChange(
    PlaylistEntryChangeStatus Status,
    IReadOnlyList<PlaylistEntryRow> Entries,
    int? Max = null)
{
    public static PlaylistEntryChange Of(PlaylistEntryChangeStatus status, int? max = null)
    {
        return new PlaylistEntryChange(status, [], max);
    }
}
