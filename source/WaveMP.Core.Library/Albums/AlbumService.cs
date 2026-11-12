using WaveMP.Core.Library.Queries;
using WaveMP.Core.Library.Tracks;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Library.Albums;

internal sealed class AlbumService(IAlbumRepository albums, ITrackRepository tracks) : IAlbumService
{
    public Task<Page<AlbumRow>> ListAsync(AlbumListRequest request, CancellationToken cancellationToken = default)
    {
        return albums.ListAsync(LibraryQueries.ToQuery(request), cancellationToken);
    }

    public Task<AlbumRow?> GetAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        return albums.FindAsync(id, userId, cancellationToken);
    }

    public async Task<Page<TrackRow>?> ListTracksAsync(
        Guid albumId,
        TrackListRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await albums.ExistsAsync(albumId, cancellationToken))
        {
            return null;
        }

        return await tracks.ListAsync(
            LibraryQueries.ToQuery(request with { AlbumId = albumId }, TrackSortField.Album),
            cancellationToken);
    }
}
