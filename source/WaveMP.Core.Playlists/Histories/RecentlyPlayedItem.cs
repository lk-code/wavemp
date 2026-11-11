using WaveMP.Data.Projections;

namespace WaveMP.Core.Playlists.Histories;

/// <summary>
/// A track of the recently played list.
/// </summary>
/// <param name="Track">The track.</param>
/// <param name="LastPlayedAt">When the user started it for the last time.</param>
public sealed record RecentlyPlayedItem(TrackRow Track, DateTimeOffset LastPlayedAt);
