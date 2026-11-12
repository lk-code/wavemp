using WaveMP.Data.Projections;

namespace WaveMP.Core.Collection.Favorites;

/// <summary>
/// A favorite with its track, album or artist; exactly the one of its type is set.
/// </summary>
/// <param name="Favorite">The favorite.</param>
/// <param name="Track">The track of a track favorite.</param>
/// <param name="Album">The album of an album favorite.</param>
/// <param name="Artist">The artist of an artist favorite.</param>
public sealed record FavoriteItem(FavoriteRow Favorite, TrackRow? Track, AlbumRow? Album, ArtistRow? Artist);
