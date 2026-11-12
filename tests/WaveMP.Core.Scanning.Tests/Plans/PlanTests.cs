using AwesomeAssertions;
using WaveMP.Core.Scanning.Discoveries;
using WaveMP.Core.Scanning.Plans;
using WaveMP.Core.Scanning.Writers;
using WaveMP.Data.Entities;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Scanning.Tests.Plans;

[TestFixture]
public class PlanTests
{
    private static readonly DateTimeOffset _time = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public void ScanScope_WithNestedFolders_KeepsOnlyTheOuterOnes()
    {
        var scope = ScanScope.Of(["Artist/Album", "Artist", "Other\\Album/", "Artist/Album/CD1", "Other/Album"]);

        scope.Directories.Should().Equal("Artist", "Other/Album");
        scope.IsFull.Should().BeFalse();
        scope.Contains("Artist/Album").Should().BeTrue();
        scope.Contains("Artists").Should().BeFalse();
    }

    [TestCase("")]
    [TestCase("/")]
    [TestCase(".")]
    public void ScanScope_WithTheMediaFolder_IsAFullScan(string root)
    {
        ScanScope.Of(["Artist", root]).IsFull.Should().BeTrue();
        ScanScope.Of([]).IsFull.Should().BeTrue();
    }

    [Test]
    public void Plan_NewChangedUnchangedAndRemovedFiles_ListsTheChangedFolderOnly()
    {
        DiscoveredDirectory changed = Directory(
            "A",
            File("A/new.mp3"),
            File("A/changed.mp3", size: 2),
            File("A/same.mp3"),
            File("A/newer.mp3", modifiedAt: _time.AddMinutes(1)));
        DiscoveredDirectory unchanged = Directory("B", File("B/same.mp3"));
        MediaFileSnapshot[] stored =
        [
            Stored("A/changed.mp3"),
            Stored("A/same.mp3"),
            Stored("A/newer.mp3"),
            Stored("A/gone.mp3"),
            Stored("B/same.mp3"),
        ];

        ScanPlan plan = ScanPlanner.Plan([changed, unchanged], stored, 1);

        DirectoryPlan directory = plan.ChangedDirectories.Should().ContainSingle().Subject;
        directory.RelativePath.Should().Be("A");
        directory.FilesToRead.Select(file => file.RelativePath)
            .Should().Equal("A/new.mp3", "A/changed.mp3", "A/newer.mp3");
        (directory.Added, directory.Updated, directory.Removed).Should().Be((1, 2, 1));
        plan.FilesUnchanged.Should().Be(2);
        plan.FilesFound.Should().Be(5);
        plan.DirectoriesFound.Should().Be(2);
        plan.DeletedDirectories.Should().BeEmpty();
    }

    [Test]
    public void Plan_FileReadByAnOlderReaderOrWithOtherKind_IsReadAgain()
    {
        ScanPlan plan = ScanPlanner.Plan(
            [Directory("A", File("A/old.mp3"), File("A/kind.mp4"))],
            [Stored("A/old.mp3", version: 0), Stored("A/kind.mp4", kind: MediaFileKind.Video)],
            1);

        plan.ChangedDirectories.Single().FilesToRead.Select(file => file.RelativePath)
            .Should().Equal("A/old.mp3", "A/kind.mp4");
    }

    [Test]
    public void Plan_VersionPerKind_ReadsOnlyTheKindsWithANewerVersionAgain()
    {
        ScanPlan plan = ScanPlanner.Plan(
            [Directory(
                "A",
                File("A/song.mp3"),
                File("A/song.mkv", kind: MediaFileKind.Video),
                File("A/song.jpg", kind: MediaFileKind.Image))],
            [
                Stored("A/song.mp3"),
                Stored("A/song.mkv", kind: MediaFileKind.Video),
                Stored("A/song.jpg", kind: MediaFileKind.Image),
            ],
            ScannerVersions.MetadataOf);

        plan.ChangedDirectories.Single().FilesToRead.Select(file => file.RelativePath).Should().Equal("A/song.mkv");
        plan.FilesUnchanged.Should().Be(2);
    }

    [TestCase(MediaFileKind.Audio, 1)]
    [TestCase(MediaFileKind.Video, 2)]
    [TestCase(MediaFileKind.Image, 1)]
    public void MetadataOf_Kind_ReturnsTheVersionOfTheKind(MediaFileKind kind, int expected)
    {
        ScannerVersions.MetadataOf(kind).Should().Be(expected);
    }

    [Test]
    public void Plan_FileKnownToBeChanged_IsReadAgainWithTheSameSizeAndTime()
    {
        ScanPlan plan = ScanPlanner.Plan(
            [Directory("A", File("A/edited.mp3"), File("A/same.mp3"))],
            [Stored("A/edited.mp3"), Stored("A/same.mp3")],
            1,
            new HashSet<string>(["A/edited.mp3", "B/other.mp3"], StringComparer.Ordinal));

        DirectoryPlan directory = plan.ChangedDirectories.Should().ContainSingle().Subject;
        directory.FilesToRead.Select(file => file.RelativePath).Should().Equal("A/edited.mp3");
        (directory.Added, directory.Updated, directory.Removed).Should().Be((0, 1, 0));
        plan.FilesUnchanged.Should().Be(1);
    }

    [Test]
    public void Plan_StoredFolderMissingOnDisk_IsDeleted()
    {
        ScanPlan plan = ScanPlanner.Plan(
            [Directory("A", File("A/same.mp3"))],
            [Stored("A/same.mp3"), Stored("Gone/one.mp3"), Stored("Gone/two.mp3")],
            1);

        plan.DeletedDirectories.Should().Equal("Gone");
        plan.FilesInDeletedDirectories.Should().Be(2);
        plan.ChangedDirectories.Should().BeEmpty();
    }

    [Test]
    public void Plan_FolderThatLostAllFiles_IsChanged()
    {
        ScanPlan plan = ScanPlanner.Plan([Directory("A")], [Stored("A/one.mp3")], 1);

        plan.ChangedDirectories.Single().Removed.Should().Be(1);
    }

    private static DiscoveredDirectory Directory(string path, params DiscoveredFile[] files)
    {
        return new DiscoveredDirectory(path, files);
    }

    private static DiscoveredFile File(
        string path,
        long size = 1,
        DateTimeOffset? modifiedAt = null,
        MediaFileKind kind = MediaFileKind.Audio)
    {
        return new DiscoveredFile(path, path[(path.LastIndexOf('/') + 1)..], kind, size, modifiedAt ?? _time);
    }

    private static MediaFileSnapshot Stored(
        string path,
        int version = 1,
        MediaFileKind kind = MediaFileKind.Audio)
    {
        return new MediaFileSnapshot(Guid.NewGuid(), path, path[..path.LastIndexOf('/')], kind, 1, _time, version);
    }
}
