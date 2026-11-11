using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WaveMP.Core.Caching.Options;
using WaveMP.Core.Caching.Stores;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Snapshots;
using WaveMP.Testing.Files;

namespace WaveMP.Core.Caching.Tests.Stores;

[TestFixture]
public class CacheStoreTests
{
    private const int Megabyte = 1_000_000;

    private TemporaryDirectory _data = null!;
    private CacheOptions _options = null!;
    private ISettingsSnapshot<CacheSettings> _settings = null!;
    private DateTimeOffset _now;
    private CacheStore _store = null!;

    [SetUp]
    public void SetUp()
    {
        _data = new TemporaryDirectory();
        _options = new CacheOptions { RootPath = _data.Combine("cache") };
        _now = new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);
        _settings = Substitute.For<ISettingsSnapshot<CacheSettings>>();
        _settings.Current.Returns(new CacheSettings { MaxSizeMegabytes = 1 });
        _store = CreateStore();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _store.CurrentTrim;
        _data.Dispose();
    }

    [Test]
    public async Task InitializeAsync_NewFolder_CreatesTheAreasAndIsAvailable()
    {
        await _store.InitializeAsync();

        _store.IsAvailable.Should().BeTrue();
        Directory.Exists(_data.Combine("cache/covers")).Should().BeTrue();
        Directory.Exists(_data.Combine("cache/transcodes")).Should().BeTrue();
        Directory.Exists(_data.Combine("cache/tmp")).Should().BeTrue();
        _store.GetAreaPath(CacheArea.Covers).Should().Be(_data.Combine("cache/covers"));
        _store.GetUsage().Should().BeEquivalentTo(new CacheUsage(
            true,
            0,
            0,
            Megabyte,
            CacheAreaUsage.Empty,
            CacheAreaUsage.Empty));
    }

    [Test]
    public async Task InitializeAsync_ExistingFiles_LoadsTheIndexAndRemovesUnfinishedFiles()
    {
        WriteFile("cache/covers/ab/picture-96.webp", 100);
        WriteFile("cache/transcodes/session/segment-1.ts", 50);
        WriteFile("cache/tmp/unfinished.webp", 10);

        await _store.InitializeAsync();

        _store.GetUsage().Should().BeEquivalentTo(new
        {
            IsAvailable = true,
            SizeBytes = 150L,
            EntryCount = 2,
            Covers = new CacheAreaUsage(100, 1),
            Transcodes = new CacheAreaUsage(50, 1),
        });
        _store.Contains(CacheArea.Covers, "ab/picture-96.webp").Should().BeTrue();
        _store.Contains(CacheArea.Transcodes, "session/segment-1.ts").Should().BeTrue();
        Directory.EnumerateFileSystemEntries(_data.Combine("cache/tmp")).Should().BeEmpty();
    }

    [Test]
    public async Task Commit_FinishedFile_MovesItIntoTheAreaAndDeliversIt()
    {
        await _store.InitializeAsync();
        string temporary = _store.CreateTemporaryPath(".webp");
        await File.WriteAllBytesAsync(temporary, [1, 2, 3]);

        bool committed = _store.Commit(CacheArea.Covers, "ab/picture-300.webp", temporary);

        committed.Should().BeTrue();
        File.Exists(temporary).Should().BeFalse();
        _store.Contains(CacheArea.Covers, "ab/picture-300.webp").Should().BeTrue();
        _store.GetUsage().Covers.Should().Be(new CacheAreaUsage(3, 1));
        await using Stream stream = _store.TryOpen(CacheArea.Covers, "ab/picture-300.webp")!;
        using MemoryStream content = new();
        await stream.CopyToAsync(content);
        content.ToArray().Should().Equal(1, 2, 3);
    }

    [Test]
    public async Task Commit_ExistingName_ReplacesTheFileAndItsSize()
    {
        await _store.InitializeAsync();
        Add(CacheArea.Covers, "ab/picture-96.webp", 100);

        Add(CacheArea.Covers, "ab/picture-96.webp", 40);

        _store.GetUsage().Covers.Should().Be(new CacheAreaUsage(40, 1));
    }

    [TestCase("../outside.webp")]
    [TestCase("ab/../../outside.webp")]
    [TestCase("")]
    [TestCase("ab//picture.webp")]
    [TestCase("ab/pic ture.webp")]
    public async Task Commit_InvalidName_StoresNothing(string name)
    {
        await _store.InitializeAsync();
        string temporary = _store.CreateTemporaryPath("webp");
        await File.WriteAllBytesAsync(temporary, [1]);

        _store.Commit(CacheArea.Covers, name, temporary).Should().BeFalse();

        File.Exists(temporary).Should().BeFalse();
        _store.GetUsage().EntryCount.Should().Be(0);
        _store.TryOpen(CacheArea.Covers, name).Should().BeNull();
    }

    [Test]
    public async Task TryOpen_FileRemovedOutsideOfWaveMp_ReturnsNullAndForgetsIt()
    {
        await _store.InitializeAsync();
        Add(CacheArea.Covers, "ab/picture-96.webp", 100);
        File.Delete(_data.Combine("cache/covers/ab/picture-96.webp"));

        Stream? stream = _store.TryOpen(CacheArea.Covers, "ab/picture-96.webp");

        stream.Should().BeNull();
        _store.Contains(CacheArea.Covers, "ab/picture-96.webp").Should().BeFalse();
        _store.GetUsage().EntryCount.Should().Be(0);
    }

    [Test]
    public async Task TryOpen_UnknownFile_ReturnsNull()
    {
        await _store.InitializeAsync();

        _store.TryOpen(CacheArea.Covers, "ab/missing.webp").Should().BeNull();
    }

    [Test]
    public async Task TryOpenAndContains_BeforeACleanUp_OnlyOpeningCountsAsUse()
    {
        await _store.InitializeAsync();
        Add(CacheArea.Covers, "aa/opened.webp", 400_000);
        _now = _now.AddMinutes(1);
        Add(CacheArea.Covers, "aa/checked.webp", 400_000);
        _now = _now.AddMinutes(1);
        _store.TryOpen(CacheArea.Covers, "aa/opened.webp")!.Dispose();
        _now = _now.AddMinutes(1);
        _store.Contains(CacheArea.Covers, "aa/checked.webp").Should().BeTrue();
        _now = _now.AddMinutes(1);

        // Exceeds the limit: the least recently used file goes.
        Add(CacheArea.Covers, "aa/new.webp", 400_000);
        await _store.CurrentTrim;

        _store.Contains(CacheArea.Covers, "aa/checked.webp").Should().BeFalse();
        _store.Contains(CacheArea.Covers, "aa/opened.webp").Should().BeTrue();
        _store.Contains(CacheArea.Covers, "aa/new.webp").Should().BeTrue();
        File.Exists(_data.Combine("cache/covers/aa/checked.webp")).Should().BeFalse();
    }

    [Test]
    public async Task TryOpen_AfterTheTouchInterval_WritesTheUseAsTimeOfTheLastChange()
    {
        await _store.InitializeAsync();
        Add(CacheArea.Covers, "ab/picture-96.webp", 10);
        string path = _data.Combine("cache/covers/ab/picture-96.webp");
        DateTime written = File.GetLastWriteTimeUtc(path);

        _now = _now.AddMinutes(30);
        _store.TryOpen(CacheArea.Covers, "ab/picture-96.webp")!.Dispose();
        DateTime afterShortTime = File.GetLastWriteTimeUtc(path);
        _now = _now.AddHours(2);
        _store.TryOpen(CacheArea.Covers, "ab/picture-96.webp")!.Dispose();

        afterShortTime.Should().Be(written);
        File.GetLastWriteTimeUtc(path).Should().Be(_now.UtcDateTime);
    }

    [Test]
    public async Task InitializeAsync_OverTheLimit_RemovesTheFilesUsedLongestAgo()
    {
        // The last use survives a restart as the time of the last change of the file.
        WriteFile("cache/covers/aa/recent.webp", 600_000);
        WriteFile("cache/covers/aa/old.webp", 600_000);
        File.SetLastWriteTimeUtc(_data.Combine("cache/covers/aa/recent.webp"), _now.AddDays(-1).UtcDateTime);
        File.SetLastWriteTimeUtc(_data.Combine("cache/covers/aa/old.webp"), _now.AddDays(-2).UtcDateTime);

        await _store.InitializeAsync();
        await _store.CurrentTrim;

        _store.Contains(CacheArea.Covers, "aa/recent.webp").Should().BeTrue();
        _store.Contains(CacheArea.Covers, "aa/old.webp").Should().BeFalse();
        _store.GetUsage().SizeBytes.Should().Be(600_000);
    }

    [Test]
    public async Task TrimAsync_OverTheLimit_RemovesTheLeastRecentlyUsedFilesDownToTheTargetShare()
    {
        await _store.InitializeAsync();
        for (int index = 1; index <= 5; index++)
        {
            Add(CacheArea.Covers, $"aa/picture-{index}.webp", 300_000);
            await _store.CurrentTrim;
            _now = _now.AddMinutes(1);
        }

        int removedLater = await _store.TrimAsync();

        _store.GetUsage().SizeBytes.Should().Be(900_000);
        _store.Contains(CacheArea.Covers, "aa/picture-1.webp").Should().BeFalse();
        _store.Contains(CacheArea.Covers, "aa/picture-2.webp").Should().BeFalse();
        _store.Contains(CacheArea.Covers, "aa/picture-3.webp").Should().BeTrue();
        _store.Contains(CacheArea.Covers, "aa/picture-5.webp").Should().BeTrue();
        removedLater.Should().Be(0);
    }

    [Test]
    public async Task RemoveAsync_MatchingNames_RemovesOnlyThoseFilesOfTheArea()
    {
        await _store.InitializeAsync();
        Add(CacheArea.Covers, "aa/old-96.webp", 10);
        Add(CacheArea.Covers, "aa/new-96.webp", 10);
        Add(CacheArea.Transcodes, "old/segment.ts", 10);

        int removed = await _store.RemoveAsync(
            CacheArea.Covers,
            name => name.Contains("old", StringComparison.Ordinal));

        removed.Should().Be(1);
        _store.Contains(CacheArea.Covers, "aa/old-96.webp").Should().BeFalse();
        _store.Contains(CacheArea.Covers, "aa/new-96.webp").Should().BeTrue();
        _store.Contains(CacheArea.Transcodes, "old/segment.ts").Should().BeTrue();
        File.Exists(_data.Combine("cache/covers/aa/old-96.webp")).Should().BeFalse();
    }

    [Test]
    public async Task ClearAsync_FilesOfAllAreas_RemovesThemButKeepsUnfinishedFiles()
    {
        await _store.InitializeAsync();
        Add(CacheArea.Covers, "aa/picture-96.webp", 10);
        Add(CacheArea.Transcodes, "session/segment.ts", 10);
        string unfinished = _store.CreateTemporaryPath(".webp");
        await File.WriteAllBytesAsync(unfinished, [1]);

        await _store.ClearAsync();

        _store.GetUsage().Should().BeEquivalentTo(new { IsAvailable = true, SizeBytes = 0L, EntryCount = 0 });
        Directory.EnumerateFiles(_data.Combine("cache/covers"), "*", SearchOption.AllDirectories).Should().BeEmpty();
        Directory.EnumerateFiles(_data.Combine("cache/transcodes"), "*", SearchOption.AllDirectories)
            .Should().BeEmpty();
        File.Exists(unfinished).Should().BeTrue();
    }

    [Test]
    public async Task InitializeAsync_FolderThatCannotBeWritten_MakesTheStoreUnavailable()
    {
        // A file where the cache folder should be: the folder cannot be created.
        await File.WriteAllBytesAsync(_data.Combine("cache"), [1]);
        string temporary = _data.Combine("finished.webp");
        await File.WriteAllBytesAsync(temporary, [1]);

        await _store.InitializeAsync();

        _store.IsAvailable.Should().BeFalse();
        _store.Commit(CacheArea.Covers, "aa/picture.webp", temporary).Should().BeFalse();
        File.Exists(temporary).Should().BeFalse();
        _store.TryOpen(CacheArea.Covers, "aa/picture.webp").Should().BeNull();
        _store.Contains(CacheArea.Covers, "aa/picture.webp").Should().BeFalse();
        _store.GetUsage().Should().BeEquivalentTo(new
        {
            IsAvailable = false,
            SizeBytes = 0L,
            MaxSizeBytes = (long)Megabyte,
        });
        (await _store.TrimAsync()).Should().Be(0);
    }

    private CacheStore CreateStore()
    {
        TimeProvider time = Substitute.For<TimeProvider>();
        time.GetUtcNow().Returns(_ => _now);
        return new CacheStore(
            Microsoft.Extensions.Options.Options.Create(_options),
            _settings,
            time,
            NullLogger<CacheStore>.Instance);
    }

    private void Add(CacheArea area, string name, int size)
    {
        string temporary = _store.CreateTemporaryPath(".tmp");
        File.WriteAllBytes(temporary, new byte[size]);
        _store.Commit(area, name, temporary).Should().BeTrue();
    }

    private void WriteFile(string relativePath, int size)
    {
        string path = _data.Combine(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, new byte[size]);
    }
}
