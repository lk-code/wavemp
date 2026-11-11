namespace WaveMP.Core.Playlists.Covers;

/// <summary>
/// The cover of a playlist.
/// </summary>
/// <param name="Kind">Whether the playlist shows its own cover, covers of its tracks or none.</param>
/// <param name="Version">Version of the own cover; changes with every upload. <c>null</c> without an own cover.
/// </param>
/// <param name="TrackIds">Tracks whose covers the playlist shows without an own cover, at most four, from different
/// albums, in the order of the playlist; empty otherwise.</param>
public sealed record PlaylistCover(PlaylistCoverKind Kind, string? Version, IReadOnlyList<Guid> TrackIds)
{
    public static PlaylistCover None { get; } = new(PlaylistCoverKind.None, null, []);
}
