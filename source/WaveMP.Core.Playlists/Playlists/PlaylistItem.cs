using WaveMP.Core.Playlists.Covers;
using WaveMP.Data.Projections;

namespace WaveMP.Core.Playlists.Playlists;

/// <summary>
/// A playlist with its cover.
/// </summary>
/// <param name="Playlist">The playlist and the summary of its entries.</param>
/// <param name="Cover">What the playlist shows as cover.</param>
public sealed record PlaylistItem(PlaylistRow Playlist, PlaylistCover Cover);
