using WaveMP.Data.Queries;

namespace WaveMP.Data.Repositories;

/// <summary>
/// Paging, sorting and filters of the track list.
/// </summary>
/// <param name="Offset">Number of tracks to skip.</param>
/// <param name="Limit">Maximum number of tracks of the page.</param>
/// <param name="Sort">Sort key, see <see cref="TrackSortField"/>; ties are sorted by id.</param>
/// <param name="Direction">Sort direction of the key; disc and track number always ascend.</param>
/// <param name="Search">Normalized search text: only tracks whose title, artist or album contains it or is similar
/// to it.</param>
/// <param name="ArtistId">Only tracks of this artist or on albums of this album artist.</param>
/// <param name="AlbumId">Only tracks of this album.</param>
/// <param name="Genre">Only tracks of this genre, without regard to case.</param>
/// <param name="Year">Only tracks of this year.</param>
/// <param name="HasAudio">Only tracks with (true) or without (false) an audio file.</param>
/// <param name="HasVideo">Only tracks with (true) or without (false) a video file.</param>
public sealed record TrackListQuery(
    int Offset,
    int Limit,
    TrackSortField Sort,
    SortDirection Direction,
    string? Search = null,
    Guid? ArtistId = null,
    Guid? AlbumId = null,
    string? Genre = null,
    int? Year = null,
    bool? HasAudio = null,
    bool? HasVideo = null);
