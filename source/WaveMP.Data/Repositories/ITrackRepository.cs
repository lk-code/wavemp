using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;

namespace WaveMP.Data.Repositories;

/// <summary>
/// Reads the tracks of the library, their files and the genres and years of the tracks. Nothing is tracked.
/// </summary>
public interface ITrackRepository
{
    /// <summary>
    /// Lists the tracks sorted and filtered as requested; ties are sorted by id.
    /// </summary>
    Task<Page<TrackRow>> ListAsync(TrackListQuery query, CancellationToken cancellationToken = default);

    Task<TrackRow?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the files of a track: audio, video and its own pictures, also duplicates of lower quality.
    /// </summary>
    Task<Page<MediaFile>> ListFilesAsync(TrackFileListQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the genres of the tracks; genres that differ only in case are one genre.
    /// </summary>
    Task<Page<GenreRow>> ListGenresAsync(GenreListQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the years of the tracks.
    /// </summary>
    Task<Page<YearRow>> ListYearsAsync(YearListQuery query, CancellationToken cancellationToken = default);
}
