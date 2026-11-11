using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Library.Searches;

internal sealed class SearchService(
    IArtistRepository artists,
    IAlbumRepository albums,
    ITrackRepository tracks) : ISearchService
{
    public async Task<SearchResult?> SearchAsync(
        string? text,
        int offset,
        int limit,
        CancellationToken cancellationToken = default)
    {
        string? search = SearchText.Normalize(text);
        if (search is null)
        {
            return null;
        }

        // One after the other: the repositories share the database context of the request.
        Page<ArtistRow> foundArtists = await artists.ListAsync(
            new ArtistListQuery(offset, limit, ArtistSortField.Relevance, SortDirection.Desc, search),
            cancellationToken);
        Page<AlbumRow> foundAlbums = await albums.ListAsync(
            new AlbumListQuery(offset, limit, AlbumSortField.Relevance, SortDirection.Desc, search),
            cancellationToken);
        Page<TrackRow> foundTracks = await tracks.ListAsync(
            new TrackListQuery(offset, limit, TrackSortField.Relevance, SortDirection.Desc, search),
            cancellationToken);

        return new SearchResult(foundArtists, foundAlbums, foundTracks);
    }
}
