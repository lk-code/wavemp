using AwesomeAssertions;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;
using WaveMP.Testing.Seeds;

namespace WaveMP.Data.Tests.Repositories;

[TestFixture]
[Category("Integration")]
public class AlbumRepositoryTests : LibraryDatabaseTests
{
    protected override string DatabaseName => "wavemp_tests_library_albums";

    [Test]
    public async Task ListAsync_Always_SummarizesTheTracksOfEachAlbum()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();

        Page<AlbumRow> page = await ListAsync(Query());

        page.Total.Should().Be(3);
        page.Items.Single(album => album.Id == library.FirstAlbum.Id).Should().BeEquivalentTo(new AlbumRow(
            library.FirstAlbum.Id,
            "First Album",
            library.Alpha.Id,
            "Alpha Band",
            false,
            2001,
            "Rock",
            3,
            TimeSpan.FromMinutes(9).TotalMilliseconds,
            true,
            library.FirstAlbum.FirstSeenAt));
        page.Items.Single(album => album.Id == library.SecondAlbum.Id).HasCover.Should().BeTrue();
        page.Items.Single(album => album.Id == library.Hits.Id).Should().BeEquivalentTo(new
        {
            ArtistId = (Guid?)null,
            ArtistName = (string?)null,
            IsCompilation = true,
            Genre = "Pop",
            TrackCount = 2,
            HasCover = false,
        });
    }

    [Test]
    public async Task ListAsync_GenreOfTheAlbum_IsTheFirstGenreByDiscAndTrackNumber()
    {
        LibrarySeed seed = new();
        Album album = seed.Album("Mixed");
        seed.Track("Third", album: album, number: 1, disc: 2, genre: "Jazz");
        seed.Track("Second", album: album, number: 2, disc: 1, genre: "Blues");
        seed.Track("First", album: album, number: 1, disc: 1);
        await SaveAsync(seed);

        Page<AlbumRow> page = await ListAsync(Query());

        page.Items.Single().Genre.Should().Be("Blues");
        page.Items.Single().DurationMilliseconds.Should().Be(0);
    }

    [Test]
    public async Task ListAsync_ByArtist_PutsCompilationsLast()
    {
        await SeedStandardLibraryAsync();

        Page<AlbumRow> ascending = await ListAsync(Query());
        Page<AlbumRow> byTitle = await ListAsync(Query(sort: AlbumSortField.Title, direction: SortDirection.Desc));
        Page<AlbumRow> byYear = await ListAsync(Query(sort: AlbumSortField.Year));

        ascending.Items.Select(album => album.Title).Should().Equal("First Album", "Second Album", "Hits Collection");
        byTitle.Items.Select(album => album.Title).Should().Equal("Second Album", "Hits Collection", "First Album");
        byYear.Items.Select(album => album.Title).Should().Equal("First Album", "Hits Collection", "Second Album");
    }

    [Test]
    public async Task ListAsync_WithArtist_FindsAlbumsOfTheArtistAndAlbumsWithItsTracks()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();

        (await TitlesAsync(Query(artistId: library.Alpha.Id))).Should().Equal("First Album", "Hits Collection");
        (await TitlesAsync(Query(artistId: library.Guest.Id))).Should().Equal("First Album");
    }

    [Test]
    public async Task ListAsync_WithGenreYearAndSearch_ReturnsTheMatchingAlbums()
    {
        await SeedStandardLibraryAsync();

        (await TitlesAsync(Query(genre: "pop"))).Should().Equal("Second Album", "Hits Collection");
        (await TitlesAsync(Query(year: 2001))).Should().Equal("First Album");
        (await TitlesAsync(Query(search: "secnd album"))).Should().Contain("Second Album");
        (await TitlesAsync(Query(search: "beta"))).Should().Equal("Second Album");
    }

    [Test]
    public async Task ListAsync_ByRelevance_PutsTheBestMatchFirst()
    {
        await SeedStandardLibraryAsync();

        Page<AlbumRow> page = await ListAsync(
            Query(search: "First Album", sort: AlbumSortField.Relevance, direction: SortDirection.Desc));

        page.Items.First().Title.Should().Be("First Album");
    }

    [Test]
    public async Task FindAsync_KnownAndUnknownId_ReturnsTheAlbumOrNull()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        AlbumRepository repository = new(CreateDbContext());

        (await repository.FindAsync(library.Hits.Id))!.Title.Should().Be("Hits Collection");
        (await repository.FindAsync(Guid.NewGuid())).Should().BeNull();
        (await repository.ExistsAsync(library.Hits.Id)).Should().BeTrue();
        (await repository.ExistsAsync(Guid.NewGuid())).Should().BeFalse();
    }

    private static AlbumListQuery Query(
        AlbumSortField sort = AlbumSortField.Artist,
        SortDirection direction = SortDirection.Asc,
        string? search = null,
        Guid? artistId = null,
        string? genre = null,
        int? year = null)
    {
        return new AlbumListQuery(0, 100, sort, direction, search, artistId, genre, year);
    }

    private async Task<Page<AlbumRow>> ListAsync(AlbumListQuery query)
    {
        return await new AlbumRepository(CreateDbContext()).ListAsync(query);
    }

    private async Task<IReadOnlyList<string>> TitlesAsync(AlbumListQuery query)
    {
        return [.. (await ListAsync(query)).Items.Select(album => album.Title)];
    }
}
