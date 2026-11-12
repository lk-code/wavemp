using WaveMP.Core.Library.Albums;
using WaveMP.Core.Library.Queries;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Library.Artists;

internal sealed class ArtistService(IArtistRepository artists, IAlbumRepository albums) : IArtistService
{
    public Task<Page<ArtistRow>> ListAsync(ArtistListRequest request, CancellationToken cancellationToken = default)
    {
        return artists.ListAsync(LibraryQueries.ToQuery(request), cancellationToken);
    }

    public Task<ArtistRow?> GetAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        return artists.FindAsync(id, userId, cancellationToken);
    }

    public async Task<Page<AlbumRow>?> ListAlbumsAsync(
        Guid artistId,
        AlbumListRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await artists.ExistsAsync(artistId, cancellationToken))
        {
            return null;
        }

        return await albums.ListAsync(
            LibraryQueries.ToQuery(request with { ArtistId = artistId }),
            cancellationToken);
    }
}
