using AwesomeAssertions;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;
using WaveMP.Testing.Seeds;

namespace WaveMP.Data.Tests.Repositories;

[TestFixture]
[Category("Integration")]
public class TrackRepositoryTests : LibraryDatabaseTests
{
    protected override string DatabaseName => "wavemp_tests_library_tracks";

    [Test]
    public async Task ListAsync_ByAlbumArtist_KeepsAlbumsTogetherAndPutsCompilationsLast()
    {
        await SeedStandardLibraryAsync();

        Page<TrackRow> page = await ListAsync(
            new TrackListQuery(0, 100, TrackSortField.AlbumArtist, SortDirection.Asc));

        page.Items.Select(track => track.Title).Should().Equal(
            "Opening", "Closing", "Duet", "Ballad", "Clip", "Loose Song", "Hit One", "Hit Two");
        page.Total.Should().Be(8);
    }

    [Test]
    public async Task ListAsync_ByAlbum_SortsByAlbumTitleAndTrackNumber()
    {
        await SeedStandardLibraryAsync();

        Page<TrackRow> page = await ListAsync(new TrackListQuery(0, 100, TrackSortField.Album, SortDirection.Asc));

        page.Items.Select(track => track.Title).Should().Equal(
            "Opening", "Closing", "Duet", "Hit One", "Hit Two", "Ballad", "Clip", "Loose Song");
    }

    [Test]
    public async Task ListAsync_ByTitleDescending_ReversesTheTitles()
    {
        await SeedStandardLibraryAsync();

        Page<TrackRow> page = await ListAsync(new TrackListQuery(0, 100, TrackSortField.Title, SortDirection.Desc));

        page.Items.Select(track => track.Title).Should().Equal(
            "Opening", "Loose Song", "Hit Two", "Hit One", "Duet", "Closing", "Clip", "Ballad");
    }

    [Test]
    public async Task ListAsync_ByArtist_SortsByTheArtistOfTheTrack()
    {
        await SeedStandardLibraryAsync();

        Page<TrackRow> page = await ListAsync(new TrackListQuery(0, 100, TrackSortField.Artist, SortDirection.Asc));

        page.Items.Select(track => track.Title).Should().Equal(
            "Opening", "Closing", "Hit One", "Hit Two", "Ballad", "Clip", "Duet", "Loose Song");
    }

    [Test]
    public async Task ListAsync_ByFirstSeenAtDescending_ShowsTheNewestFirstAndKeepsAlbumsTogether()
    {
        await SeedStandardLibraryAsync();

        Page<TrackRow> page = await ListAsync(
            new TrackListQuery(0, 100, TrackSortField.FirstSeenAt, SortDirection.Desc));

        page.Items.Select(track => track.Title).Should().Equal(
            "Hit One", "Hit Two", "Clip", "Loose Song", "Ballad", "Opening", "Closing", "Duet");
    }

    [Test]
    public async Task ListAsync_ByDurationAndYear_SortsByTheKey()
    {
        await SeedStandardLibraryAsync();

        Page<TrackRow> byDuration = await ListAsync(
            new TrackListQuery(0, 100, TrackSortField.Duration, SortDirection.Desc, HasAudio: true));
        Page<TrackRow> byYear = await ListAsync(new TrackListQuery(0, 100, TrackSortField.Year, SortDirection.Asc));

        byDuration.Items.First().Title.Should().Be("Ballad");
        byYear.Items.Select(track => track.Title).Take(6).Should().Equal(
            "Opening", "Closing", "Duet", "Hit One", "Hit Two", "Ballad");
    }

    [Test]
    public async Task ListAsync_WithPages_ReturnsEveryTrackExactlyOnce()
    {
        await SeedStandardLibraryAsync();
        List<string> titles = [];

        for (int offset = 0; offset < 8; offset += 3)
        {
            Page<TrackRow> page = await ListAsync(
                new TrackListQuery(offset, 3, TrackSortField.Genre, SortDirection.Asc));
            page.Total.Should().Be(8);
            titles.AddRange(page.Items.Select(track => track.Title));
        }

        titles.Should().HaveCount(8).And.OnlyHaveUniqueItems();
    }

    [Test]
    public async Task ListAsync_WithArtist_FindsTracksOfTheArtistAndOnAlbumsOfTheArtist()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();

        Page<TrackRow> alpha = await ListAsync(Query(artistId: library.Alpha.Id));
        Page<TrackRow> guest = await ListAsync(Query(artistId: library.Guest.Id));

        alpha.Items.Select(track => track.Title).Should().Equal("Opening", "Closing", "Duet", "Hit One");
        guest.Items.Select(track => track.Title).Should().Equal("Duet", "Loose Song");
    }

    [Test]
    public async Task ListAsync_WithFilters_ReturnsTheMatchingTracks()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();

        (await TitlesAsync(Query(albumId: library.SecondAlbum.Id))).Should().Equal("Ballad");
        (await TitlesAsync(Query(genre: "ROCK"))).Should().Equal("Opening", "Closing", "Duet");
        (await TitlesAsync(Query(year: 2005))).Should().Equal("Hit One", "Hit Two");
        (await TitlesAsync(Query(hasVideo: true))).Should().Equal("Closing", "Clip");
        (await TitlesAsync(Query(hasAudio: false))).Should().Equal("Clip");
        (await TitlesAsync(Query(genre: "Ro%"))).Should().BeEmpty();
    }

    [TestCase("Closng", "Closing")]
    [TestCase("clos", "Closing")]
    [TestCase("balad", "Ballad")]
    [TestCase("opening", "Opening")]
    public async Task ListAsync_WithSearch_FindsTitlesDespiteTyposAndParts(string search, string expected)
    {
        await SeedStandardLibraryAsync();

        IReadOnlyList<string> titles = await TitlesAsync(Query(search: search));

        titles.Should().Contain(expected);
    }

    [Test]
    public async Task ListAsync_WithSearch_FindsTracksByArtistAndAlbum()
    {
        await SeedStandardLibraryAsync();

        (await TitlesAsync(Query(search: "Hits Colection"))).Should().BeEquivalentTo("Hit One", "Hit Two");
        (await TitlesAsync(Query(search: "Guest Star"))).Should().BeEquivalentTo("Duet", "Loose Song");
    }

    [Test]
    public async Task ListAsync_ByRelevance_PutsTheBestMatchFirst()
    {
        await SeedStandardLibraryAsync();

        Page<TrackRow> page = await ListAsync(
            new TrackListQuery(0, 100, TrackSortField.Relevance, SortDirection.Desc, Search: "Hit Two"));

        page.Items.First().Title.Should().Be("Hit Two");
    }

    [Test]
    public async Task ListAsync_WithWildcardsInTheSearch_MatchesThemLiterally()
    {
        LibrarySeed seed = new();
        seed.Track("Under_score");
        seed.Track("Plain");
        await SaveAsync(seed);

        (await TitlesAsync(Query(search: "_"))).Should().Equal("Under_score");
        (await TitlesAsync(Query(search: "%"))).Should().BeEmpty();
    }

    [Test]
    public async Task ListAsync_Always_ReturnsArtistAlbumAndCover()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();

        IReadOnlyList<TrackRow> tracks = (await ListAsync(Query())).Items;

        TrackRow duet = tracks.Single(track => track.Title == "Duet");
        duet.Should().BeEquivalentTo(new
        {
            ArtistId = library.Guest.Id,
            ArtistName = "Guest Star",
            AlbumId = library.FirstAlbum.Id,
            AlbumTitle = "First Album",
            AlbumArtistId = library.Alpha.Id,
            AlbumArtistName = "Alpha Band",
            AlbumIsCompilation = false,
            TrackNumber = 3,
            DiscNumber = 1,
            Year = 2001,
            Genre = "Rock",
            Duration = TimeSpan.FromMinutes(2),
            HasAudio = true,
            HasVideo = false,
            HasCover = true,
        });
        tracks.Single(track => track.Title == "Hit One").Should().BeEquivalentTo(new
        {
            AlbumArtistId = (Guid?)null,
            AlbumIsCompilation = true,
            HasCover = false,
        });
        tracks.Single(track => track.Title == "Ballad").HasCover.Should().BeTrue();
        tracks.Single(track => track.Title == "Clip").Should().BeEquivalentTo(new
        {
            AlbumId = (Guid?)null,
            HasAudio = false,
            HasVideo = true,
            HasCover = false,
        });
    }

    [Test]
    public async Task ListAsync_TrackOfAnAlbumWhoseOtherTrackHasACover_HasACover()
    {
        LibrarySeed seed = new();
        Album album = seed.Album("Album");
        Track first = seed.Track("First", album: album, number: 1);
        seed.Track("Second", album: album, number: 2);
        seed.TrackCover(first, seed.File("Album/01 First.jpg", MediaFileKind.Image, first));
        await SaveAsync(seed);

        IReadOnlyList<TrackRow> tracks = (await ListAsync(Query())).Items;

        tracks.Should().OnlyContain(track => track.HasCover);
    }

    [Test]
    public async Task FindAsync_KnownAndUnknownId_ReturnsTheTrackOrNull()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        TrackRepository repository = new(CreateDbContext());

        (await repository.FindAsync(library.Ballad.Id))!.Title.Should().Be("Ballad");
        (await repository.FindAsync(Guid.NewGuid())).Should().BeNull();
        (await repository.ExistsAsync(library.Clip.Id)).Should().BeTrue();
        (await repository.ExistsAsync(Guid.NewGuid())).Should().BeFalse();
    }

    [Test]
    public async Task ListFilesAsync_ByKind_ListsAudioVideoAndPicturesWithThePrimaryFileFirst()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        TrackRepository repository = new(CreateDbContext());

        Page<MediaFile> files = await repository.ListFilesAsync(
            new TrackFileListQuery(library.Closing.Id, 0, 100, MediaFileSortField.Kind, SortDirection.Asc));
        Page<MediaFile> videos = await repository.ListFilesAsync(new TrackFileListQuery(
            library.Closing.Id, 0, 100, MediaFileSortField.Kind, SortDirection.Asc, MediaFileKind.Video));
        Page<MediaFile> bySize = await repository.ListFilesAsync(
            new TrackFileListQuery(library.Closing.Id, 0, 100, MediaFileSortField.Size, SortDirection.Desc));

        files.Items.Select(file => file.FileName).Should().Equal(
            "02 - Closing.flac", "02 - Closing.mp3", "02 - Closing.mkv", "02 - Closing.jpg");
        videos.Items.Select(file => file.FileName).Should().Equal("02 - Closing.mkv");
        bySize.Items.First().FileName.Should().Be("02 - Closing.mkv");
    }

    [Test]
    public async Task ListGenresAsync_Always_GroupsGenresWithoutRegardToCase()
    {
        await SeedStandardLibraryAsync();
        TrackRepository repository = new(CreateDbContext());

        Page<GenreRow> byName = await repository.ListGenresAsync(
            new GenreListQuery(0, 100, GenreSortField.Name, SortDirection.Asc));
        Page<GenreRow> byCount = await repository.ListGenresAsync(
            new GenreListQuery(0, 100, GenreSortField.TrackCount, SortDirection.Desc));
        Page<GenreRow> search = await repository.ListGenresAsync(
            new GenreListQuery(0, 100, GenreSortField.Name, SortDirection.Asc, "rok"));

        byName.Items.Should().BeEquivalentTo(
            [new GenreRow("Pop", 3, 2), new GenreRow("Rock", 3, 1)],
            options => options.WithStrictOrdering());
        byName.Total.Should().Be(2);
        byCount.Items.Select(genre => genre.Name).Should().Equal("Pop", "Rock");
        search.Items.Select(genre => genre.Name).Should().Equal("Rock");
    }

    [Test]
    public async Task ListYearsAsync_Always_CountsTracksAndAlbumsPerYear()
    {
        await SeedStandardLibraryAsync();
        TrackRepository repository = new(CreateDbContext());

        Page<YearRow> years = await repository.ListYearsAsync(new YearListQuery(0, 100, SortDirection.Desc));

        years.Items.Should().BeEquivalentTo(
            [new YearRow(2010, 1, 1), new YearRow(2005, 2, 1), new YearRow(2001, 3, 1)],
            options => options.WithStrictOrdering());
    }

    [Test]
    public async Task FindPrimaryFileAsync_WithDuplicates_ReturnsThePrimaryFileOfTheKind()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        TrackRepository repository = new(CreateDbContext());

        TrackFileRow? audio = await repository.FindPrimaryFileAsync(library.Closing.Id, MediaFileKind.Audio);
        TrackFileRow? video = await repository.FindPrimaryFileAsync(library.Closing.Id, MediaFileKind.Video);

        audio!.TrackId.Should().Be(library.Closing.Id);
        audio.File!.RelativePath.Should().Be("Alpha Band/First Album/02 - Closing.flac");
        video!.File!.RelativePath.Should().Be("Alpha Band/First Album/02 - Closing.mkv");
    }

    [Test]
    public async Task FindPrimaryFileAsync_TrackWithoutAudio_ReturnsTheTrackWithoutFile()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();

        TrackFileRow? row = await new TrackRepository(CreateDbContext())
            .FindPrimaryFileAsync(library.Clip.Id, MediaFileKind.Audio);

        row.Should().NotBeNull();
        row!.File.Should().BeNull();
    }

    [Test]
    public async Task FindPrimaryFileAsync_UnknownTrack_ReturnsNull()
    {
        await SeedStandardLibraryAsync();

        TrackFileRow? row = await new TrackRepository(CreateDbContext())
            .FindPrimaryFileAsync(Guid.NewGuid(), MediaFileKind.Audio);

        row.Should().BeNull();
    }

    [Test]
    public async Task ListByIdsAsync_Always_ReturnsTheExistingTracksOnce()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();

        IReadOnlyList<TrackRow> rows = await new TrackRepository(CreateDbContext()).ListByIdsAsync(
            [library.Opening.Id, library.Ballad.Id, library.Opening.Id, Guid.NewGuid()]);

        rows.Select(row => row.Title).Should().BeEquivalentTo(["Opening", "Ballad"]);
        rows.Single(row => row.Title == "Ballad").HasCover.Should().BeTrue();
    }

    [Test]
    public async Task ListByIdsAsync_WithoutIds_ReturnsNothing()
    {
        IReadOnlyList<TrackRow> rows = await new TrackRepository(CreateDbContext()).ListByIdsAsync([]);

        rows.Should().BeEmpty();
    }

    private static TrackListQuery Query(
        string? search = null,
        Guid? artistId = null,
        Guid? albumId = null,
        string? genre = null,
        int? year = null,
        bool? hasAudio = null,
        bool? hasVideo = null)
    {
        return new TrackListQuery(
            0,
            100,
            TrackSortField.AlbumArtist,
            SortDirection.Asc,
            search,
            artistId,
            albumId,
            genre,
            year,
            hasAudio,
            hasVideo);
    }

    private async Task<Page<TrackRow>> ListAsync(TrackListQuery query)
    {
        return await new TrackRepository(CreateDbContext()).ListAsync(query);
    }

    private async Task<IReadOnlyList<string>> TitlesAsync(TrackListQuery query)
    {
        return [.. (await ListAsync(query)).Items.Select(track => track.Title)];
    }
}
