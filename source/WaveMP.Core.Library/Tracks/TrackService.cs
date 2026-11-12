using WaveMP.Core.Library.Queries;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Library.Tracks;

internal sealed class TrackService(ITrackRepository tracks) : ITrackService
{
    public Task<Page<TrackRow>> ListAsync(TrackListRequest request, CancellationToken cancellationToken = default)
    {
        return tracks.ListAsync(LibraryQueries.ToQuery(request, TrackSortField.AlbumArtist), cancellationToken);
    }

    public Task<TrackRow?> GetAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        return tracks.FindAsync(id, userId, cancellationToken);
    }

    public async Task<Page<MediaFile>?> ListFilesAsync(
        Guid trackId,
        TrackFileListRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await tracks.ExistsAsync(trackId, cancellationToken))
        {
            return null;
        }

        return await tracks.ListFilesAsync(LibraryQueries.ToQuery(trackId, request), cancellationToken);
    }

    public Task<Page<GenreRow>> ListGenresAsync(
        GenreListRequest request,
        CancellationToken cancellationToken = default)
    {
        return tracks.ListGenresAsync(LibraryQueries.ToQuery(request), cancellationToken);
    }

    public Task<Page<YearRow>> ListYearsAsync(YearListRequest request, CancellationToken cancellationToken = default)
    {
        return tracks.ListYearsAsync(LibraryQueries.ToQuery(request), cancellationToken);
    }
}
