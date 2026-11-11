using WaveMP.Data.Projections;

namespace WaveMP.Core.Library.Covers;

/// <summary>
/// Finds the covers of tracks and albums following the cover rule of the library settings. Reading the pictures is the
/// task of <see cref="ICoverSourceReader"/>.
/// </summary>
public interface ICoverService
{
    /// <summary>
    /// The covers that may be shown for a track, the best first: by default the cover of its album, then its own,
    /// then the covers of the other tracks of its album.
    /// </summary>
    Task<CoverCandidates> GetTrackCandidatesAsync(Guid trackId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The covers that may be shown for an album, the best first: by default the picture in its folder, then the
    /// covers of its tracks.
    /// </summary>
    Task<CoverCandidates> GetAlbumCandidatesAsync(Guid albumId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The ordered lists of covers that the library shows: per album the list of the album, of every track with covers
    /// of its own and of its tracks without one, then the list of every track without album. Identical lists come
    /// only once, empty lists never. Reads the library in batches.
    /// </summary>
    IAsyncEnumerable<IReadOnlyList<CoverCandidateRow>> EnumerateShownCoversAsync(
        CancellationToken cancellationToken = default);
}
