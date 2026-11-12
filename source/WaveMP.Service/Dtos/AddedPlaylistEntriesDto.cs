namespace WaveMP.Service.Dtos;

/// <summary>
/// The entries added to a playlist and the playlist afterwards.
/// </summary>
public sealed class AddedPlaylistEntriesDto
{
    /// <summary>
    /// The new entries, in the order of the request.
    /// </summary>
    public required IReadOnlyList<PlaylistEntryDto> Entries { get; init; }

    public required PlaylistDto Playlist { get; init; }
}
