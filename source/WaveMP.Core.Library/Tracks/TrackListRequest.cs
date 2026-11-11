using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Library.Tracks;

/// <summary>
/// A request for a page of tracks as the client sends it.
/// </summary>
/// <param name="Offset">Number of tracks to skip.</param>
/// <param name="Limit">Maximum number of tracks of the page.</param>
/// <param name="Sort">Sort key; by default the album artist (the album in the tracks of an album), or the relevance
/// with a search text.</param>
/// <param name="Direction">Sort direction; by default descending for the relevance and the time found first.</param>
/// <param name="Search">Search text as typed; empty texts search nothing.</param>
/// <param name="ArtistId">Only tracks of this artist.</param>
/// <param name="AlbumId">Only tracks of this album.</param>
/// <param name="Genre">Only tracks of this genre; empty texts filter nothing.</param>
/// <param name="Year">Only tracks of this year.</param>
/// <param name="HasAudio">Only tracks with or without an audio file.</param>
/// <param name="HasVideo">Only tracks with or without a video file.</param>
public sealed record TrackListRequest(
    int Offset,
    int Limit,
    TrackSortField? Sort = null,
    SortDirection? Direction = null,
    string? Search = null,
    Guid? ArtistId = null,
    Guid? AlbumId = null,
    string? Genre = null,
    int? Year = null,
    bool? HasAudio = null,
    bool? HasVideo = null);
