using AwesomeAssertions;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Data.Tests.Repositories;

[TestFixture]
[Category("Integration")]
public class ArtistRepositoryTests : LibraryDatabaseTests
{
    protected override string DatabaseName => "wavemp_tests_library_artists";

    [Test]
    public async Task ListAsync_ByName_CountsAlbumsAndTracksOfEachArtist()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();

        Page<ArtistRow> page = await ListAsync(new ArtistListQuery(0, 100, ArtistSortField.Name, SortDirection.Asc));

        page.Items.Should().BeEquivalentTo(
            [
                new ArtistRow(library.Alpha.Id, "Alpha Band", 2, 4, library.Alpha.FirstSeenAt),
                new ArtistRow(library.Beta.Id, "Beta Singer", 2, 3, library.Beta.FirstSeenAt),
                new ArtistRow(library.Guest.Id, "Guest Star", 1, 2, library.Guest.FirstSeenAt),
            ],
            options => options.WithStrictOrdering());
    }

    [Test]
    public async Task ListAsync_ByFirstSeenAtDescending_ShowsTheNewestFirst()
    {
        await SeedStandardLibraryAsync();

        Page<ArtistRow> page = await ListAsync(
            new ArtistListQuery(0, 100, ArtistSortField.FirstSeenAt, SortDirection.Desc));

        page.Items.Select(artist => artist.Name).Should().Equal("Beta Singer", "Alpha Band", "Guest Star");
    }

    [TestCase("alpa band", "Alpha Band")]
    [TestCase("SINGER", "Beta Singer")]
    [TestCase("gest star", "Guest Star")]
    public async Task ListAsync_WithSearch_FindsTheArtistDespiteTypos(string search, string expected)
    {
        await SeedStandardLibraryAsync();

        Page<ArtistRow> page = await ListAsync(
            new ArtistListQuery(0, 100, ArtistSortField.Relevance, SortDirection.Desc, search));

        page.Items.First().Name.Should().Be(expected);
    }

    [Test]
    public async Task FindAsync_KnownAndUnknownId_ReturnsTheArtistOrNull()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        ArtistRepository repository = new(CreateDbContext());

        (await repository.FindAsync(library.Guest.Id))!.TrackCount.Should().Be(2);
        (await repository.FindAsync(Guid.NewGuid())).Should().BeNull();
        (await repository.ExistsAsync(library.Beta.Id)).Should().BeTrue();
        (await repository.ExistsAsync(Guid.NewGuid())).Should().BeFalse();
    }

    private async Task<Page<ArtistRow>> ListAsync(ArtistListQuery query)
    {
        return await new ArtistRepository(CreateDbContext()).ListAsync(query);
    }
}
