using WaveMP.Data.Entities;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Library.Tracks;

/// <summary>
/// A request for a page of the files of a track.
/// </summary>
/// <param name="Offset">Number of files to skip.</param>
/// <param name="Limit">Maximum number of files of the page.</param>
/// <param name="Sort">Sort key; by default the kind (audio, video, pictures).</param>
/// <param name="Direction">Sort direction; ascending by default.</param>
/// <param name="Kind">Only files of this kind.</param>
public sealed record TrackFileListRequest(
    int Offset,
    int Limit,
    MediaFileSortField? Sort = null,
    SortDirection? Direction = null,
    MediaFileKind? Kind = null);
