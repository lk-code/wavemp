using WaveMP.Data.Projections;
using WaveMP.Data.Queries;

namespace WaveMP.Core.Library.Searches;

/// <summary>
/// The artists, albums and tracks that match a search text, each sorted by relevance.
/// </summary>
public sealed record SearchResult(Page<ArtistRow> Artists, Page<AlbumRow> Albums, Page<TrackRow> Tracks);
