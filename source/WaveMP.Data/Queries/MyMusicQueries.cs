using WaveMP.Data.Entities;

namespace WaveMP.Data.Queries;

/// <summary>
/// The "my music" of a user: the tracks the user rated, marked as favorite (also through the favorite album or artist
/// of the track or of its album) or added to one of the own playlists.
/// </summary>
internal static class MyMusicQueries
{
    /// <summary>
    /// Only the tracks of the "my music" of the user.
    /// </summary>
    public static IQueryable<Track> Filter(IQueryable<Track> tracks, WaveMpDbContext dbContext, Guid userId)
    {
        // One EXISTS per kind, so each one can use its index.
        return tracks.Where(track =>
            dbContext.TrackRatings.Any(rating => rating.UserId == userId && rating.TrackId == track.Id)
            || dbContext.Favorites.Any(favorite => favorite.UserId == userId && favorite.TrackId == track.Id)
            || (track.AlbumId != null
                && dbContext.Favorites.Any(favorite => favorite.UserId == userId && favorite.AlbumId == track.AlbumId))
            || (track.ArtistId != null
                && dbContext.Favorites.Any(favorite =>
                    favorite.UserId == userId && favorite.ArtistId == track.ArtistId))
            || (track.Album != null
                && track.Album.AlbumArtistId != null
                && dbContext.Favorites.Any(favorite =>
                    favorite.UserId == userId && favorite.ArtistId == track.Album.AlbumArtistId))
            || dbContext.PlaylistEntries.Any(entry => entry.TrackId == track.Id && entry.Playlist!.UserId == userId));
    }

    /// <summary>
    /// All tracks of the "my music" of the user.
    /// </summary>
    public static IQueryable<Track> Tracks(WaveMpDbContext dbContext, Guid userId)
    {
        return Filter(dbContext.Tracks, dbContext, userId);
    }

    /// <summary>
    /// The user a filter or sort key needs.
    /// </summary>
    public static Guid RequireUser(Guid? userId, string what)
    {
        return userId ?? throw new ArgumentException($"{what} needs a user.", nameof(userId));
    }
}
