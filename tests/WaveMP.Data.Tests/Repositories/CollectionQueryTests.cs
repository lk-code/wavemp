using AwesomeAssertions;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;
using WaveMP.Testing.Seeds;

namespace WaveMP.Data.Tests.Repositories;

/// <summary>
/// Favorites, ratings and "my music" of a user in the track, album, artist, genre and year lists.
/// </summary>
[TestFixture]
[Category("Integration")]
public class CollectionQueryTests : LibraryDatabaseTests
{
    private static readonly DateTimeOffset _start = new(2026, 11, 1, 20, 0, 0, TimeSpan.Zero);

    protected override string DatabaseName => "wavemp_tests_collection_queries";

    [Test]
    public async Task TrackRows_Always_CarryTheFavoriteAndRatingOfTheUser()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        LibrarySeed seed = new();
        User user = seed.User();
        User other = seed.User();
        seed.Favorite(user, library.Ballad);
        seed.Rating(user, library.Opening, 4);
        seed.Rating(other, library.Ballad, 2);
        await SaveAsync(seed);

        IReadOnlyList<TrackRow> own = (await ListTracksAsync(Query(userId: user.Id))).Items;
        IReadOnlyList<TrackRow> foreign = (await ListTracksAsync(Query(userId: other.Id))).Items;
        IReadOnlyList<TrackRow> anonymous = (await ListTracksAsync(Query())).Items;

        own.Single(row => row.Id == library.Ballad.Id).Should().BeEquivalentTo(new { IsFavorite = true, Rating = 0 });
        own.Single(row => row.Id == library.Opening.Id).Should().BeEquivalentTo(new { IsFavorite = false, Rating = 4 });
        foreign.Single(row => row.Id == library.Ballad.Id).Should().BeEquivalentTo(
            new { IsFavorite = false, Rating = 2 });
        foreign.Single(row => row.Id == library.Opening.Id).Rating.Should().Be(0);
        anonymous.Should().AllSatisfy(row => row.Should().BeEquivalentTo(new { IsFavorite = false, Rating = 0 }));
    }

    [Test]
    public async Task FindAndListByIds_WithUser_CarryTheFavoriteAndRating()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        LibrarySeed seed = new();
        User user = seed.User();
        seed.Favorite(user, library.Ballad);
        seed.Rating(user, library.Ballad, 5);
        await SaveAsync(seed);

        TrackRow? found = await new TrackRepository(CreateDbContext()).FindAsync(library.Ballad.Id, user.Id);
        IReadOnlyList<TrackRow> listed = await new TrackRepository(CreateDbContext())
            .ListByIdsAsync([library.Ballad.Id], user.Id);
        TrackRow? anonymous = await new TrackRepository(CreateDbContext()).FindAsync(library.Ballad.Id);

        found.Should().BeEquivalentTo(new { IsFavorite = true, Rating = 5 });
        listed.Should().ContainSingle().Which.Should().BeEquivalentTo(new { IsFavorite = true, Rating = 5 });
        anonymous.Should().BeEquivalentTo(new { IsFavorite = false, Rating = 0 });
    }

    [Test]
    public async Task ListTracks_MyMusic_ContainsRatedFavoriteAndOwnPlaylistTracks()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await SeedMyMusicAsync(library);

        IReadOnlyList<string> titles = await TrackTitlesAsync(Query(userId: user.Id, myMusic: true));

        // Closing is in no list of the user, Hit Two only in the playlist of another user.
        titles.Should().BeEquivalentTo(["Opening", "Duet", "Ballad", "Clip", "Loose Song", "Hit One"]);
    }

    [Test]
    public async Task ListTracks_MyMusicWithFavoriteArtist_ContainsTheTracksOfTheArtistAndOfItsAlbums()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        LibrarySeed seed = new();
        User user = seed.User();
        seed.Favorite(user, library.Alpha);
        await SaveAsync(seed);

        IReadOnlyList<string> titles = await TrackTitlesAsync(Query(userId: user.Id, myMusic: true));

        // Duet is by Guest Star, but on the album of Alpha Band.
        titles.Should().Equal("Opening", "Closing", "Duet", "Hit One");
    }

    [Test]
    public async Task ListTracks_MyMusic_CombinesWithTheOtherFilters()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await SeedMyMusicAsync(library);

        (await TrackTitlesAsync(Query(userId: user.Id, myMusic: true, genre: "ROCK")))
            .Should().Equal("Opening", "Duet");
        (await TrackTitlesAsync(Query(userId: user.Id, myMusic: true, hasAudio: false))).Should().Equal("Clip");
        (await TrackTitlesAsync(Query(userId: user.Id, myMusic: true, year: 2005))).Should().Equal("Hit One");
        (await TrackTitlesAsync(Query(userId: user.Id, myMusic: true, search: "song"))).Should().Equal("Loose Song");
        (await TrackTitlesAsync(Query(userId: user.Id, myMusic: true, artistId: library.Beta.Id)))
            .Should().Equal("Ballad", "Clip");
    }

    [Test]
    public async Task ListTracks_ByRating_SortsByStarsThenByTheLatestRating()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        LibrarySeed seed = new();
        User user = seed.User();
        User other = seed.User();
        seed.Rating(user, library.Ballad, 5, _start);
        seed.Rating(user, library.Opening, 3, _start.AddMinutes(1));
        seed.Rating(user, library.Duet, 5, _start.AddMinutes(2));
        seed.Rating(other, library.Clip, 5, _start);
        await SaveAsync(seed);

        IReadOnlyList<string> all = await TrackTitlesAsync(
            new TrackListQuery(0, 100, TrackSortField.Rating, SortDirection.Desc, UserId: user.Id));
        IReadOnlyList<string> rated = await TrackTitlesAsync(
            new TrackListQuery(0, 100, TrackSortField.Rating, SortDirection.Desc, UserId: user.Id, MinRating: 1));
        IReadOnlyList<string> ascending = await TrackTitlesAsync(
            new TrackListQuery(0, 100, TrackSortField.Rating, SortDirection.Asc, UserId: user.Id, MinRating: 4));

        all.Take(3).Should().Equal("Duet", "Ballad", "Opening");
        all.Should().HaveCount(8);
        rated.Should().Equal("Duet", "Ballad", "Opening");
        ascending.Should().Equal("Duet", "Ballad");
    }

    [Test]
    public async Task ListTracks_MyMusicOrRatingWithoutUser_Throws()
    {
        Func<Task> myMusic = () => ListTracksAsync(Query(myMusic: true));
        Func<Task> rating = () => ListTracksAsync(new TrackListQuery(0, 10, TrackSortField.Rating, SortDirection.Desc));

        await myMusic.Should().ThrowAsync<ArgumentException>();
        await rating.Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    public async Task ListAlbums_Always_CarryTheFavoriteAndFilterMyMusic()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await SeedMyMusicAsync(library);
        LibrarySeed seed = new();
        seed.Favorite(user, library.FirstAlbum);
        await SaveAsync(seed);

        Page<AlbumRow> all = await new AlbumRepository(CreateDbContext()).ListAsync(
            new AlbumListQuery(0, 100, AlbumSortField.Title, SortDirection.Asc, UserId: user.Id));
        Page<AlbumRow> mine = await new AlbumRepository(CreateDbContext()).ListAsync(
            new AlbumListQuery(
                0,
                100,
                AlbumSortField.Title,
                SortDirection.Asc,
                Year: 2010,
                UserId: user.Id,
                MyMusic: true));
        IReadOnlyList<AlbumRow> byIds = await new AlbumRepository(CreateDbContext()).ListByIdsAsync(
            [library.FirstAlbum.Id, library.Hits.Id, Guid.NewGuid()],
            user.Id);

        all.Items.Select(album => (album.Title, album.IsFavorite)).Should().Equal(
            ("First Album", true),
            ("Hits Collection", false),
            ("Second Album", true));
        mine.Items.Select(album => album.Title).Should().Equal("Second Album");
        byIds.Select(album => album.Title).Should().BeEquivalentTo(["First Album", "Hits Collection"]);
        byIds.Single(album => album.Id == library.FirstAlbum.Id).IsFavorite.Should().BeTrue();
        (await new AlbumRepository(CreateDbContext()).FindAsync(library.SecondAlbum.Id, user.Id))!
            .IsFavorite.Should().BeTrue();
    }

    [Test]
    public async Task ListAlbums_MyMusic_ContainsAlbumsWithTracksOfMyMusic()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        LibrarySeed seed = new();
        User user = seed.User();
        seed.Rating(user, library.Ballad, 4);
        seed.Favorite(user, library.Clip);
        seed.Playlist(user, "Mix", library.HitOne);
        await SaveAsync(seed);

        Page<AlbumRow> page = await new AlbumRepository(CreateDbContext()).ListAsync(
            new AlbumListQuery(0, 100, AlbumSortField.Title, SortDirection.Asc, UserId: user.Id, MyMusic: true));

        page.Items.Select(album => album.Title).Should().Equal("Hits Collection", "Second Album");
    }

    [Test]
    public async Task ListArtists_Always_CarryTheFavoriteAndFilterMyMusic()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        LibrarySeed seed = new();
        User user = seed.User();
        seed.Favorite(user, library.FirstAlbum);
        seed.Favorite(user, library.Beta);
        await SaveAsync(seed);
        LibrarySeed other = new();
        User stranger = other.User();
        other.Rating(stranger, library.HitTwo, 4);
        await SaveAsync(other);

        Page<ArtistRow> mine = await new ArtistRepository(CreateDbContext()).ListAsync(
            new ArtistListQuery(0, 100, ArtistSortField.Name, SortDirection.Asc, UserId: user.Id, MyMusic: true));
        Page<ArtistRow> strangers = await new ArtistRepository(CreateDbContext()).ListAsync(
            new ArtistListQuery(
                0,
                100,
                ArtistSortField.Name,
                SortDirection.Asc,
                UserId: stranger.Id,
                MyMusic: true));
        IReadOnlyList<ArtistRow> byIds = await new ArtistRepository(CreateDbContext())
            .ListByIdsAsync([library.Beta.Id, library.Guest.Id], user.Id);

        // The album brings Alpha Band and Guest Star (Duet), the favorite artist Beta Singer.
        mine.Items.Select(artist => (artist.Name, artist.IsFavorite)).Should().Equal(
            ("Alpha Band", false),
            ("Beta Singer", true),
            ("Guest Star", false));
        strangers.Items.Select(artist => artist.Name).Should().Equal("Beta Singer");
        byIds.Select(artist => (artist.Name, artist.IsFavorite))
            .Should().BeEquivalentTo([("Beta Singer", true), ("Guest Star", false)]);
        (await new ArtistRepository(CreateDbContext()).FindAsync(library.Beta.Id, user.Id))!
            .IsFavorite.Should().BeTrue();
        (await new ArtistRepository(CreateDbContext()).FindAsync(library.Beta.Id))!
            .IsFavorite.Should().BeFalse();
    }

    [Test]
    public async Task ListGenresAndYears_MyMusic_CountOnlyTheTracksOfMyMusic()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        LibrarySeed seed = new();
        User user = seed.User();
        seed.Rating(user, library.Opening, 4);
        seed.Rating(user, library.Ballad, 3);
        seed.Rating(user, library.HitTwo, 5);
        await SaveAsync(seed);
        TrackRepository repository = new(CreateDbContext());

        Page<GenreRow> genres = await repository.ListGenresAsync(
            new GenreListQuery(0, 100, GenreSortField.Name, SortDirection.Asc, UserId: user.Id, MyMusic: true));
        Page<YearRow> years = await repository.ListYearsAsync(
            new YearListQuery(0, 100, SortDirection.Desc, user.Id, MyMusic: true));

        genres.Items.Should().Equal(new GenreRow("Pop", 2, 2), new GenreRow("Rock", 1, 1));
        years.Items.Should().Equal(new YearRow(2010, 1, 1), new YearRow(2005, 1, 1), new YearRow(2001, 1, 1));
    }

    /// <summary>
    /// The user rates Opening, marks Clip, Second Album (Ballad) and Guest Star (Duet, Loose Song) as favorites and
    /// has Hit One in a playlist; another user has Hit Two in a playlist.
    /// </summary>
    private async Task<User> SeedMyMusicAsync(StandardLibrary library)
    {
        LibrarySeed seed = new();
        User user = seed.User();
        User other = seed.User();
        seed.Rating(user, library.Opening, 2);
        seed.Favorite(user, library.Clip);
        seed.Favorite(user, library.SecondAlbum);
        seed.Favorite(user, library.Guest);
        seed.Playlist(user, "Mine", library.HitOne);
        seed.Playlist(other, "Theirs", library.HitTwo, library.Closing);
        seed.Rating(other, library.Closing, 5);
        await SaveAsync(seed);
        return user;
    }

    private static TrackListQuery Query(
        Guid? userId = null,
        bool myMusic = false,
        string? search = null,
        Guid? artistId = null,
        string? genre = null,
        int? year = null,
        bool? hasAudio = null)
    {
        return new TrackListQuery(
            0,
            100,
            TrackSortField.AlbumArtist,
            SortDirection.Asc,
            search,
            artistId,
            Genre: genre,
            Year: year,
            HasAudio: hasAudio,
            UserId: userId,
            MyMusic: myMusic);
    }

    private async Task<Page<TrackRow>> ListTracksAsync(TrackListQuery query)
    {
        return await new TrackRepository(CreateDbContext()).ListAsync(query);
    }

    private async Task<IReadOnlyList<string>> TrackTitlesAsync(TrackListQuery query)
    {
        return [.. (await ListTracksAsync(query)).Items.Select(track => track.Title)];
    }
}
