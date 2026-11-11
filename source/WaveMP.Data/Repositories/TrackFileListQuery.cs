using WaveMP.Data.Entities;
using WaveMP.Data.Queries;

namespace WaveMP.Data.Repositories;

/// <summary>
/// Paging, sorting and filter of the files of a track.
/// </summary>
/// <param name="TrackId">Id of the track.</param>
/// <param name="Offset">Number of files to skip.</param>
/// <param name="Limit">Maximum number of files of the page.</param>
/// <param name="Sort">Sort key; ties are sorted by name and id.</param>
/// <param name="Direction">Sort direction of the key.</param>
/// <param name="Kind">Only files of this kind.</param>
public sealed record TrackFileListQuery(
    Guid TrackId,
    int Offset,
    int Limit,
    MediaFileSortField Sort,
    SortDirection Direction,
    MediaFileKind? Kind = null);
