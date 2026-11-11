using AwesomeAssertions;
using WaveMP.Data.Entities;
using WaveMP.Data.Repositories;
using WaveMP.Testing.Seeds;

namespace WaveMP.Data.Tests.Repositories;

[TestFixture]
[Category("Integration")]
public class LibraryScanRepositoryTests : LibraryDatabaseTests
{
    protected override string DatabaseName => "wavemp_tests_library_scan";

    [Test]
    public async Task GetFolderSnapshotsAsync_Folder_ReturnsOnlyItsOwnFiles()
    {
        await SeedFilesAsync();

        IReadOnlyList<MediaFileSnapshot> snapshots = await CreateRepository().GetFolderSnapshotsAsync("Band/Album");

        snapshots.Select(snapshot => snapshot.RelativePath)
            .Should().BeEquivalentTo("Band/Album/01 - One.mp3", "Band/Album/cover.jpg");
        snapshots.Should().AllSatisfy(snapshot => snapshot.DirectoryPath.Should().Be("Band/Album"));
    }

    [Test]
    public async Task GetFolderSnapshotsAsync_MediaFolder_ReturnsTheFilesOfTheRootOnly()
    {
        await SeedFilesAsync();

        IReadOnlyList<MediaFileSnapshot> snapshots = await CreateRepository().GetFolderSnapshotsAsync(string.Empty);

        snapshots.Select(snapshot => snapshot.RelativePath).Should().Equal("Loose.mp3");
    }

    [Test]
    public async Task GetFolderSnapshotsAsync_UnknownFolder_ReturnsNothing()
    {
        await SeedFilesAsync();

        IReadOnlyList<MediaFileSnapshot> snapshots = await CreateRepository().GetFolderSnapshotsAsync("Band/Alb");

        snapshots.Should().BeEmpty();
    }

    private async Task SeedFilesAsync()
    {
        LibrarySeed seed = new();
        seed.File("Loose.mp3", MediaFileKind.Audio);
        seed.File("Band/Album/01 - One.mp3", MediaFileKind.Audio);
        seed.File("Band/Album/cover.jpg", MediaFileKind.Image, isPrimary: false);
        seed.File("Band/Album/CD2/01 - Two.mp3", MediaFileKind.Audio);
        seed.File("Band/Album Two/01 - Three.mp3", MediaFileKind.Audio);
        await SaveAsync(seed);
    }

    private LibraryScanRepository CreateRepository()
    {
        return new LibraryScanRepository(CreateDbContext());
    }
}
