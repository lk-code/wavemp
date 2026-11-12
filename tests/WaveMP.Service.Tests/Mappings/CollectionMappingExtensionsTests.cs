using AwesomeAssertions;
using WaveMP.Core.Collection.Favorites;
using WaveMP.Core.Collection.HomePages;
using WaveMP.Core.Playlists.Covers;
using WaveMP.Core.Playlists.Histories;
using WaveMP.Core.Playlists.Playlists;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Service.Dtos;
using WaveMP.Service.Mappings;

namespace WaveMP.Service.Tests.Mappings;

[TestFixture]
public class CollectionMappingExtensionsTests
{
    private static readonly DateTimeOffset _time = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);

    [Test]
    public void ToDto_TrackFavorite_CarriesOnlyTheTrack()
    {
        TrackRow track = Track(rating: 4);
        FavoriteRow favorite = new(Guid.NewGuid(), FavoriteType.Track, track.Id, _time);

        FavoriteDto dto = new FavoriteItem(favorite, track, null, null).ToDto();

        dto.Type.Should().Be(FavoriteTypeDto.Track);
        dto.ItemId.Should().Be(track.Id);
        dto.AddedAt.Should().Be(_time);
        dto.Track.Should().BeEquivalentTo(track.ToDto());
        dto.Track!.IsFavorite.Should().BeTrue();
        dto.Track.Rating.Should().Be(4);
        dto.Album.Should().BeNull();
        dto.Artist.Should().BeNull();
    }

    [Test]
    public void ToDto_AlbumAndArtistFavorites_CarryTheirItems()
    {
        AlbumRow album = new(Guid.NewGuid(), "First Album", null, null, false, 2001, null, 2, 1000, false, _time, true);
        ArtistRow artist = new(Guid.NewGuid(), "Alpha Band", 1, 2, _time, true);

        FavoriteDto albumDto = new FavoriteItem(
            new FavoriteRow(Guid.NewGuid(), FavoriteType.Album, album.Id, _time),
            null,
            album,
            null).ToDto();
        FavoriteDto artistDto = new FavoriteItem(
            new FavoriteRow(Guid.NewGuid(), FavoriteType.Artist, artist.Id, _time),
            null,
            null,
            artist).ToDto();

        albumDto.Type.Should().Be(FavoriteTypeDto.Album);
        albumDto.Album!.IsFavorite.Should().BeTrue();
        albumDto.Track.Should().BeNull();
        artistDto.Type.Should().Be(FavoriteTypeDto.Artist);
        artistDto.Artist!.IsFavorite.Should().BeTrue();
        artistDto.Album.Should().BeNull();
    }

    [Test]
    public void ToDto_HomePage_MapsEveryRow()
    {
        TrackRow track = Track(rating: 5);
        PlaylistRow playlist = new(Guid.NewGuid(), "Mix", 1, 1000, null, _time, _time);
        HomePage page = new(
            new Page<PlaylistItem>(
                [new PlaylistItem(playlist, new PlaylistCover(PlaylistCoverKind.Tracks, null, [track.Id]))],
                0,
                20,
                1),
            new Page<RecentlyPlayedItem>([new RecentlyPlayedItem(track, _time)], 0, 20, 3),
            new Page<AlbumRow>([], 0, 20, 0),
            new Page<FavoriteItem>([], 0, 20, 0),
            new Page<TrackRow>([track], 0, 20, 1));

        HomeDto dto = page.ToDto();

        dto.Playlists.Items.Should().ContainSingle().Which.Name.Should().Be("Mix");
        dto.RecentlyPlayed.Total.Should().Be(3);
        dto.RecentlyPlayed.Items.Should().ContainSingle().Which.Track.Id.Should().Be(track.Id);
        dto.RecentlyAdded.Items.Should().BeEmpty();
        dto.Favorites.Limit.Should().Be(20);
        dto.TopRated.Items.Should().ContainSingle().Which.Rating.Should().Be(5);
    }

    [Test]
    public void ToEntity_AllFavoriteTypes_AreMappedBothWays()
    {
        Enum.GetValues<FavoriteTypeDto>().Select(type => type.ToEntity().ToDto())
            .Should().Equal(Enum.GetValues<FavoriteTypeDto>());
        Enum.GetValues<FavoriteType>().Select(type => type.ToDto().ToEntity())
            .Should().Equal(Enum.GetValues<FavoriteType>());
    }

    private static TrackRow Track(int rating)
    {
        return new TrackRow(
            Guid.NewGuid(),
            "Song",
            null,
            "Band",
            null,
            null,
            null,
            null,
            false,
            1,
            null,
            2001,
            null,
            TimeSpan.FromSeconds(3),
            true,
            false,
            false,
            _time,
            true,
            rating);
    }
}
