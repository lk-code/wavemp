using WaveMP.Data.Projections;

namespace WaveMP.Core.Playlists.Playlists;

/// <summary>
/// An entry of a playlist with its track.
/// </summary>
/// <param name="Entry">The entry with its position.</param>
/// <param name="Track">The track of the entry.</param>
public sealed record PlaylistEntryItem(PlaylistEntryRow Entry, TrackRow Track);
