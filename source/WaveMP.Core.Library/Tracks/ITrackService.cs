using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;

namespace WaveMP.Core.Library.Tracks;

/// <summary>
/// The tracks of the library, their files and the genres and years of the tracks.
/// </summary>
public interface ITrackService
{
    Task<Page<TrackRow>> ListAsync(TrackListRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// The track with the favorite and rating of the profile <paramref name="userId"/>.
    /// </summary>
    Task<TrackRow?> GetAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// The files of a track; <c>null</c> if the track does not exist.
    /// </summary>
    Task<Page<MediaFile>?> ListFilesAsync(
        Guid trackId,
        TrackFileListRequest request,
        CancellationToken cancellationToken = default);

    Task<Page<GenreRow>> ListGenresAsync(GenreListRequest request, CancellationToken cancellationToken = default);

    Task<Page<YearRow>> ListYearsAsync(YearListRequest request, CancellationToken cancellationToken = default);
}
