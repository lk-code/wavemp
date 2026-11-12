using WaveMP.Core.Collection.Favorites;
using WaveMP.Core.Collection.HomePages;
using WaveMP.Data.Entities;
using WaveMP.Service.Dtos;

namespace WaveMP.Service.Mappings;

public static class CollectionMappingExtensions
{
    public static FavoriteDto ToDto(this FavoriteItem item)
    {
        return new FavoriteDto
        {
            Type = item.Favorite.Type.ToDto(),
            ItemId = item.Favorite.ItemId,
            AddedAt = item.Favorite.CreatedAt,
            Track = item.Track?.ToDto(),
            Album = item.Album?.ToDto(),
            Artist = item.Artist?.ToDto(),
        };
    }

    public static HomeDto ToDto(this HomePage page)
    {
        return new HomeDto
        {
            Playlists = page.Playlists.ToDto(playlist => playlist.ToDto()),
            RecentlyPlayed = page.RecentlyPlayed.ToDto(item => item.ToDto()),
            RecentlyAdded = page.RecentlyAdded.ToDto(album => album.ToDto()),
            Favorites = page.Favorites.ToDto(favorite => favorite.ToDto()),
            TopRated = page.TopRated.ToDto(track => track.ToDto()),
        };
    }

    public static FavoriteTypeDto ToDto(this FavoriteType type)
    {
        return type switch
        {
            FavoriteType.Track => FavoriteTypeDto.Track,
            FavoriteType.Album => FavoriteTypeDto.Album,
            FavoriteType.Artist => FavoriteTypeDto.Artist,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown favorite type."),
        };
    }

    public static FavoriteType ToEntity(this FavoriteTypeDto type)
    {
        return type switch
        {
            FavoriteTypeDto.Track => FavoriteType.Track,
            FavoriteTypeDto.Album => FavoriteType.Album,
            FavoriteTypeDto.Artist => FavoriteType.Artist,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown favorite type."),
        };
    }
}
