using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using WaveMP.Service.Dtos;
using WaveMP.Service.Tests.IntegrationTests.Extensions;
using WaveMP.Testing.Files;
using WaveMP.Testing.Media;

namespace WaveMP.Service.Tests.IntegrationTests;

/// <summary>
/// The collection of profiles through the API: favorites, ratings, the "my music" filter of the library and the home
/// page, with a small library of synthetic media.
/// </summary>
[TestFixture]
[Category("Integration")]
public class CollectionApiTests
{
    private const string Favorites = "/api/v1/me/favorites";

    private TemporaryDirectory _media = null!;
    private WaveMpWebApplicationFactory _factory = null!;
    private HttpClient _anonymous = null!;
    private HttpClient _admin = null!;
    private IReadOnlyList<TrackDto> _tracks = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _media = new TemporaryDirectory();
        await CreateTrackAsync("Alpha", "One", "First", "Rock", "2001");
        await CreateTrackAsync("Alpha", "One", "Second", "Rock", "2001");
        await CreateTrackAsync("Beta", "Two", "Third", "Pop", "2005");
        await CreateTrackAsync("Gamma", "Three", "Fourth", "Jazz", "2010");

        _factory = new WaveMpWebApplicationFactory(
            WaveMpWebApplicationFactory.TestDatabaseName + "_collection",
            freshDatabase: true,
            mediaPath: _media.Path);
        _anonymous = _factory.CreateClient();
        _admin = await _factory.CreateAdminClientAsync();

        HttpResponseMessage response = await _admin.PostAsync("/api/v1/scans", null);
        ScanDto queued = (await response.Content.ReadFromJsonAsync<ScanDto>(HttpClientExtensions.JsonOptions))!;
        ScanDto scan = await _admin.WaitForScanAsync(queued.Id);
        scan.Status.Should().Be(JobRunStatusDto.Succeeded);

        _tracks = (await _admin.GetJsonAsync<PageDto<TrackDto>>("/api/v1/tracks?limit=100")).Items;
        _tracks.Should().HaveCount(4);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _admin?.Dispose();
        _anonymous?.Dispose();
        await _factory.DeleteDatabaseAsync();
        await _factory.DisposeAsync();
        _media.Dispose();
    }

    [Test]
    public async Task Collection_WithoutToken_ReturnsUnauthorized()
    {
        HttpResponseMessage listed = await _anonymous.GetAsync(Favorites);
        HttpResponseMessage added = await _anonymous.PutAsync($"{Favorites}/tracks/{Track("First").Id}", null);
        HttpResponseMessage removed = await _anonymous.DeleteAsync($"{Favorites}/albums/{Guid.NewGuid()}");
        HttpResponseMessage rated = await _anonymous.PutAsJsonAsync(
            $"/api/v1/tracks/{Track("First").Id}/rating",
            new { rating = 3 });
        HttpResponseMessage home = await _anonymous.GetAsync("/api/v1/me/home");

        await listed.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
        await added.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
        await removed.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
        await rated.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
        await home.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
    }

    [Test]
    public async Task AddMyFavorites_OfEveryType_ListsThemTheNewestFirst()
    {
        using HttpClient client = await CreateProfileAsync();
        TrackDto first = Track("First");

        await MarkAsync(client, "tracks", first.Id);
        await MarkAsync(client, "albums", first.Album!.Id);
        await MarkAsync(client, "artists", Track("Third").Artist!.Id);
        // Marking again keeps the favorite and its time.
        await MarkAsync(client, "tracks", first.Id);

        PageDto<FavoriteDto> page = await client.GetJsonAsync<PageDto<FavoriteDto>>(Favorites);

        page.Total.Should().Be(3);
        page.Items.Select(favorite => favorite.Type)
            .Should().Equal(FavoriteTypeDto.Artist, FavoriteTypeDto.Album, FavoriteTypeDto.Track);
        page.Items[0].Artist!.Name.Should().Be("Beta");
        page.Items[0].Artist!.IsFavorite.Should().BeTrue();
        page.Items[1].Album!.Title.Should().Be("One");
        page.Items[1].Album!.IsFavorite.Should().BeTrue();
        page.Items[2].ItemId.Should().Be(first.Id);
        page.Items[2].Track!.IsFavorite.Should().BeTrue();
        page.Items[2].Album.Should().BeNull();
        PageDto<FavoriteDto> albums = await client.GetJsonAsync<PageDto<FavoriteDto>>($"{Favorites}?type=Album");
        albums.Items.Should().ContainSingle().Which.ItemId.Should().Be(first.Album!.Id);
    }

    [TestCase("tracks", "track.not_found")]
    [TestCase("albums", "album.not_found")]
    [TestCase("artists", "artist.not_found")]
    public async Task AddMyFavorite_UnknownItem_ReturnsNotFound(string type, string code)
    {
        using HttpClient client = await CreateProfileAsync();

        HttpResponseMessage response = await client.PutAsync($"{Favorites}/{type}/{Guid.NewGuid()}", null);

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, code);
    }

    [Test]
    public async Task RemoveMyFavorite_Twice_RemovesItAndSucceedsAgain()
    {
        using HttpClient client = await CreateProfileAsync();
        Guid albumId = Track("Third").Album!.Id;
        await MarkAsync(client, "albums", albumId);

        HttpResponseMessage removed = await client.DeleteAsync($"{Favorites}/albums/{albumId}");
        HttpResponseMessage again = await client.DeleteAsync($"{Favorites}/albums/{albumId}");
        HttpResponseMessage unknown = await client.DeleteAsync($"{Favorites}/tracks/{Guid.NewGuid()}");

        removed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        again.StatusCode.Should().Be(HttpStatusCode.NoContent);
        unknown.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetJsonAsync<PageDto<FavoriteDto>>(Favorites)).Total.Should().Be(0);
        (await client.GetJsonAsync<AlbumDto>($"/api/v1/albums/{albumId}")).IsFavorite.Should().BeFalse();
    }

    [Test]
    public async Task ListMyFavorites_InvalidType_ReturnsValidationProblem()
    {
        using HttpClient client = await CreateProfileAsync();

        HttpResponseMessage response = await client.GetAsync($"{Favorites}?type=Playlist");

        IReadOnlyList<ValidationErrorDto> errors = await response.ShouldBeValidationProblemAsync();
        errors.Should().ContainSingle().Which.Field.Should().Be("type");
    }

    [Test]
    public async Task Favorites_OfAnotherProfile_StayHidden()
    {
        using HttpClient owner = await CreateProfileAsync();
        using HttpClient other = await CreateProfileAsync();
        TrackDto first = Track("First");
        await MarkAsync(owner, "tracks", first.Id);
        await RateAsync(owner, first.Id, 5);

        PageDto<FavoriteDto> favorites = await other.GetJsonAsync<PageDto<FavoriteDto>>(Favorites);
        TrackDto track = await other.GetJsonAsync<TrackDto>($"/api/v1/tracks/{first.Id}");
        HomeDto home = await other.GetJsonAsync<HomeDto>("/api/v1/me/home");

        favorites.Total.Should().Be(0);
        track.IsFavorite.Should().BeFalse();
        track.Rating.Should().Be(0);
        home.TopRated.Total.Should().Be(0);
    }

    [Test]
    public async Task SetMyTrackRating_ValidStars_RatesAndZeroRemovesIt()
    {
        using HttpClient client = await CreateProfileAsync();
        Guid id = Track("Second").Id;

        await RateAsync(client, id, 4);
        (await client.GetJsonAsync<TrackDto>($"/api/v1/tracks/{id}")).Rating.Should().Be(4);
        await RateAsync(client, id, 2);
        (await client.GetJsonAsync<TrackDto>($"/api/v1/tracks/{id}")).Rating.Should().Be(2);
        await RateAsync(client, id, 0);
        (await client.GetJsonAsync<TrackDto>($"/api/v1/tracks/{id}")).Rating.Should().Be(0);
    }

    [TestCase("{\"rating\":6}", ValidationErrorCodeDto.OutOfRange)]
    [TestCase("{\"rating\":-1}", ValidationErrorCodeDto.OutOfRange)]
    [TestCase("{\"rating\":null}", ValidationErrorCodeDto.Required)]
    [TestCase("{}", ValidationErrorCodeDto.Required)]
    public async Task SetMyTrackRating_InvalidRating_ReturnsTheCodeOfTheField(
        string body,
        ValidationErrorCodeDto code)
    {
        using HttpClient client = await CreateProfileAsync();

        HttpResponseMessage response = await client.SendJsonAsync(
            "PUT",
            $"/api/v1/tracks/{Track("First").Id}/rating",
            body);

        IReadOnlyList<ValidationErrorDto> errors = await response.ShouldBeValidationProblemAsync();
        ValidationErrorDto error = errors.Should().ContainSingle().Which;
        error.Field.Should().Be("rating");
        error.Code.Should().Be(code);
        if (code == ValidationErrorCodeDto.OutOfRange)
        {
            error.Min.Should().Be(0);
            error.Max.Should().Be(5);
        }
    }

    [Test]
    public async Task SetMyTrackRating_UnknownTrack_ReturnsNotFound()
    {
        using HttpClient client = await CreateProfileAsync();

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/v1/tracks/{Guid.NewGuid()}/rating",
            new { rating = 3 });

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "track.not_found");
    }

    [Test]
    public async Task Tracks_OfTheProfile_CarryFavoriteAndRatingEverywhere()
    {
        using HttpClient client = await CreateProfileAsync();
        TrackDto first = Track("First");
        await MarkAsync(client, "tracks", first.Id);
        await RateAsync(client, first.Id, 3);
        await PlayAsync(client, first.Id);
        PlaylistDto playlist = await CreatePlaylistAsync(client, first.Id);

        List<TrackDto> seen =
        [
            (await client.GetJsonAsync<PageDto<TrackDto>>("/api/v1/tracks?q=First")).Items.Single(),
            await client.GetJsonAsync<TrackDto>($"/api/v1/tracks/{first.Id}"),
            (await client.GetJsonAsync<PageDto<TrackDto>>($"/api/v1/albums/{first.Album!.Id}/tracks")).Items
                .Single(track => track.Id == first.Id),
            (await client.GetJsonAsync<SearchResultDto>("/api/v1/search?q=First")).Tracks.Items.Single(),
            (await client.GetJsonAsync<PageDto<PlaylistEntryDto>>($"/api/v1/me/playlists/{playlist.Id}/tracks"))
                .Items.Single().Track,
            (await client.GetJsonAsync<PageDto<RecentlyPlayedTrackDto>>("/api/v1/me/recently-played")).Items
                .Single().Track,
            (await client.GetJsonAsync<PageDto<PlayHistoryEntryDto>>("/api/v1/me/history")).Items.Single().Track,
        ];

        seen.Should().AllSatisfy(track =>
        {
            track.IsFavorite.Should().BeTrue();
            track.Rating.Should().Be(3);
        });
    }

    [Test]
    public async Task AlbumsAndArtists_OfTheProfile_CarryTheFavorite()
    {
        using HttpClient client = await CreateProfileAsync();
        TrackDto third = Track("Third");
        await MarkAsync(client, "albums", third.Album!.Id);
        await MarkAsync(client, "artists", third.Artist!.Id);

        AlbumDto album = await client.GetJsonAsync<AlbumDto>($"/api/v1/albums/{third.Album.Id}");
        ArtistDto artist = await client.GetJsonAsync<ArtistDto>($"/api/v1/artists/{third.Artist.Id}");
        PageDto<AlbumDto> albums = await client.GetJsonAsync<PageDto<AlbumDto>>("/api/v1/albums");
        PageDto<AlbumDto> artistAlbums =
            await client.GetJsonAsync<PageDto<AlbumDto>>($"/api/v1/artists/{third.Artist.Id}/albums");
        SearchResultDto search = await client.GetJsonAsync<SearchResultDto>("/api/v1/search?q=Beta");

        album.IsFavorite.Should().BeTrue();
        artist.IsFavorite.Should().BeTrue();
        albums.Items.Where(item => item.IsFavorite).Select(item => item.Title).Should().Equal("Two");
        artistAlbums.Items.Should().ContainSingle().Which.IsFavorite.Should().BeTrue();
        search.Artists.Items.Should().ContainSingle().Which.IsFavorite.Should().BeTrue();
    }

    [Test]
    public async Task MyMusic_OfTheProfile_LimitsEveryList()
    {
        using HttpClient client = await CreateProfileAsync();
        // First by its rating, Third by the favorite of its artist; Second and Fourth stay outside.
        await RateAsync(client, Track("First").Id, 2);
        await MarkAsync(client, "artists", Track("Third").Artist!.Id);

        PageDto<TrackDto> tracks = await client.GetJsonAsync<PageDto<TrackDto>>("/api/v1/tracks?myMusic=true");
        PageDto<TrackDto> rock =
            await client.GetJsonAsync<PageDto<TrackDto>>("/api/v1/tracks?myMusic=true&genre=Rock");
        PageDto<AlbumDto> albums = await client.GetJsonAsync<PageDto<AlbumDto>>("/api/v1/albums?myMusic=true");
        PageDto<ArtistDto> artists = await client.GetJsonAsync<PageDto<ArtistDto>>("/api/v1/artists?myMusic=true");
        PageDto<GenreDto> genres = await client.GetJsonAsync<PageDto<GenreDto>>("/api/v1/genres?myMusic=true");
        PageDto<YearDto> years = await client.GetJsonAsync<PageDto<YearDto>>("/api/v1/years?myMusic=true");
        PageDto<TrackDto> all = await client.GetJsonAsync<PageDto<TrackDto>>("/api/v1/tracks?myMusic=false");

        tracks.Items.Select(track => track.Title).Should().BeEquivalentTo("First", "Third");
        rock.Items.Select(track => track.Title).Should().Equal("First");
        albums.Items.Select(album => album.Title).Should().BeEquivalentTo("One", "Two");
        artists.Items.Select(artist => artist.Name).Should().BeEquivalentTo("Alpha", "Beta");
        genres.Items.Should().BeEquivalentTo(
            [new { Name = "Pop", TrackCount = 1 }, new { Name = "Rock", TrackCount = 1 }]);
        years.Items.Select(year => year.Year).Should().Equal(2005, 2001);
        all.Total.Should().Be(4);
    }

    [Test]
    public async Task ListTracks_SortedByRating_PutsTheMostStarsFirst()
    {
        using HttpClient client = await CreateProfileAsync();
        await RateAsync(client, Track("Second").Id, 2);
        await RateAsync(client, Track("Fourth").Id, 5);

        PageDto<TrackDto> page = await client.GetJsonAsync<PageDto<TrackDto>>("/api/v1/tracks?sort=Rating");
        PageDto<TrackDto> ascending =
            await client.GetJsonAsync<PageDto<TrackDto>>("/api/v1/tracks?sort=Rating&direction=Asc");

        page.Items.Select(track => track.Rating).Should().Equal(5, 2, 0, 0);
        page.Items[0].Title.Should().Be("Fourth");
        ascending.Items.Select(track => track.Rating).Should().Equal(0, 0, 2, 5);
    }

    [Test]
    public async Task GetMyHome_WithLimit_ReturnsTheFirstPageOfEveryRow()
    {
        using HttpClient client = await CreateProfileAsync();
        await CreatePlaylistAsync(client, Track("First").Id);
        await PlayAsync(client, Track("Second").Id);
        await MarkAsync(client, "tracks", Track("Third").Id);
        await MarkAsync(client, "artists", Track("Fourth").Artist!.Id);
        await RateAsync(client, Track("First").Id, 3);
        await RateAsync(client, Track("Fourth").Id, 5);

        HomeDto home = await client.GetJsonAsync<HomeDto>("/api/v1/me/home?limit=1");

        home.Playlists.Total.Should().Be(1);
        home.RecentlyPlayed.Items.Should().ContainSingle().Which.Track.Title.Should().Be("Second");
        home.RecentlyAdded.Limit.Should().Be(1);
        home.RecentlyAdded.Total.Should().Be(3);
        home.RecentlyAdded.Items.Should().ContainSingle();
        home.Favorites.Total.Should().Be(2);
        home.Favorites.Items.Should().ContainSingle().Which.Type.Should().Be(FavoriteTypeDto.Artist);
        home.TopRated.Total.Should().Be(2);
        home.TopRated.Items.Should().ContainSingle().Which.Title.Should().Be("Fourth");
    }

    [TestCase(0)]
    [TestCase(51)]
    public async Task GetMyHome_InvalidLimit_ReturnsValidationProblem(int limit)
    {
        using HttpClient client = await CreateProfileAsync();

        HttpResponseMessage response = await client.GetAsync($"/api/v1/me/home?limit={limit}");

        IReadOnlyList<ValidationErrorDto> errors = await response.ShouldBeValidationProblemAsync();
        errors.Should().ContainSingle().Which.Code.Should().Be(ValidationErrorCodeDto.OutOfRange);
    }

    private TrackDto Track(string title)
    {
        return _tracks.Single(track => track.Title == title);
    }

    private async Task CreateTrackAsync(string artist, string album, string title, string genre, string date)
    {
        await TestMediaGenerator.CreateAudioAsync(
            _media.Combine($"{artist}/{album}/{title}.mp3"),
            AudioFormat.Mp3,
            new TestTags { Title = title, Artist = artist, Album = album, Genre = genre, Date = date });
    }

    private async Task<HttpClient> CreateProfileAsync()
    {
        UserDto user = await _admin.CreateUserAsync(new { name = $"Collection {HttpClientExtensions.Unique()}" });
        CreatedSessionDto session = await _anonymous.CreateSessionAsync(user.Id);
        return _factory.CreateAuthorizedClient(session.Token);
    }

    private static async Task MarkAsync(HttpClient client, string type, Guid id)
    {
        HttpResponseMessage response = await client.PutAsync($"{Favorites}/{type}/{id}", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
    }

    private static async Task RateAsync(HttpClient client, Guid trackId, int rating)
    {
        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/v1/tracks/{trackId}/rating", new { rating });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
    }

    private static async Task PlayAsync(HttpClient client, Guid trackId)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/me/history",
            new { trackId, playedAt = DateTimeOffset.UtcNow.AddMinutes(-1) });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
    }

    private static async Task<PlaylistDto> CreatePlaylistAsync(HttpClient client, params Guid[] trackIds)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/me/playlists",
            new { name = "Mix", trackIds });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<PlaylistDto>(HttpClientExtensions.JsonOptions))!;
    }
}
