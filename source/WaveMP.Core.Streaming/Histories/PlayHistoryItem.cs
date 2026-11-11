using WaveMP.Data.Entities;
using WaveMP.Data.Projections;

namespace WaveMP.Core.Streaming.Histories;

/// <summary>
/// An entry of the play history with its track.
/// </summary>
/// <param name="Entry">The entry.</param>
/// <param name="Track">The track that was played.</param>
public sealed record PlayHistoryItem(PlayHistoryEntry Entry, TrackRow Track);
