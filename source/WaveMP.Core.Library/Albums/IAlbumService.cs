using WaveMP.Core.Library.Tracks;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;

namespace WaveMP.Core.Library.Albums;

/// <summary>
/// The albums of the library.
/// </summary>
public interface IAlbumService
{
    Task<Page<AlbumRow>> ListAsync(AlbumListRequest request, CancellationToken cancellationToken = default);

    Task<AlbumRow?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// The tracks of an album, by default in the order of disc and track number. <c>null</c> if the album does not
    /// exist; an album filter of the request is replaced.
    /// </summary>
    Task<Page<TrackRow>?> ListTracksAsync(
        Guid albumId,
        TrackListRequest request,
        CancellationToken cancellationToken = default);
}
