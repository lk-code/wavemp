namespace WaveMP.Core.Playlists.Playlists;

/// <summary>
/// Entries of a playlist after a request or a change, or why there are none.
/// </summary>
/// <param name="Status">Whether the request succeeded.</param>
/// <param name="Entries">The requested, added or moved entries with their positions; empty otherwise.</param>
/// <param name="Playlist">The playlist after adding entries.</param>
/// <param name="Max">The largest allowed position for <see cref="PlaylistStatus.InvalidPosition"/>, the largest
/// number of entries for <see cref="PlaylistStatus.Full"/>.</param>
public sealed record PlaylistEntriesResult(
    PlaylistStatus Status,
    IReadOnlyList<PlaylistEntryItem> Entries,
    PlaylistItem? Playlist = null,
    int? Max = null)
{
    public bool IsOk => Status == PlaylistStatus.Ok;

    public static PlaylistEntriesResult Of(PlaylistStatus status, int? max = null)
    {
        return new PlaylistEntriesResult(status, [], null, max);
    }
}
