using WaveMP.Core.Library.Albums;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;

namespace WaveMP.Core.Library.Artists;

/// <summary>
/// The artists of the library.
/// </summary>
public interface IArtistService
{
    Task<Page<ArtistRow>> ListAsync(ArtistListRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// The artist with the favorite of the profile <paramref name="userId"/>.
    /// </summary>
    Task<ArtistRow?> GetAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// The albums of an artist: with the artist as album artist or with tracks of the artist. <c>null</c> if the
    /// artist does not exist; an artist filter of the request is replaced.
    /// </summary>
    Task<Page<AlbumRow>?> ListAlbumsAsync(
        Guid artistId,
        AlbumListRequest request,
        CancellationToken cancellationToken = default);
}
