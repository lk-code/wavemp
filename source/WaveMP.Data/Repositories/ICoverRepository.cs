using WaveMP.Data.Projections;

namespace WaveMP.Data.Repositories;

/// <summary>
/// Finds the covers that may be shown for a track or an album. Nothing is tracked.
/// </summary>
public interface ICoverRepository
{
    /// <summary>
    /// The covers of the track, of its album and of the other tracks of its album, in no particular order; <c>null</c>
    /// if the track does not exist.
    /// </summary>
    Task<IReadOnlyList<CoverCandidateRow>?> GetTrackCandidatesAsync(
        Guid trackId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The covers of the album and of its tracks, in no particular order; <c>null</c> if the album does not exist.
    /// </summary>
    Task<IReadOnlyList<CoverCandidateRow>?> GetAlbumCandidatesAsync(
        Guid albumId,
        CancellationToken cancellationToken = default);
}
