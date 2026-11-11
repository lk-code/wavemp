using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WaveMP.Core.Jobs.Definitions;
using WaveMP.Core.Jobs.Runs;
using WaveMP.Core.Jobs.Services;
using WaveMP.Core.Scanning.Options;
using WaveMP.Core.Scanning.Watchers;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Snapshots;
using WaveMP.Data.Entities;
using WaveMP.Testing.Files;

namespace WaveMP.Core.Scanning.Tests.Watchers;

/// <summary>
/// Watches a real temporary folder with a short debounce time.
/// </summary>
[TestFixture]
public class MediaFolderWatcherTests
{
    private static readonly TimeSpan _wait = TimeSpan.FromSeconds(10);

    private TemporaryDirectory _media = null!;
    private IJobService _jobs = null!;
    private JobRunTracker _tracker = null!;
    private ISettingsSnapshot<LibrarySettings> _settings = null!;
    private ServiceProvider _services = null!;
    private MediaFolderWatcher _watcher = null!;

    [SetUp]
    public void SetUp()
    {
        _media = new TemporaryDirectory();
        _media.CreateDirectory("Artist/Album");
        _media.CreateDirectory("Other");
        _jobs = Substitute.For<IJobService>();
        _jobs.StartRunAsync(Arg.Any<JobRunRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => JobRunStartResult.Success(new JobRun
            {
                Id = Guid.NewGuid(),
                JobId = JobIds.LibraryScan,
                Trigger = JobRunTrigger.FileWatcher,
                Status = JobRunStatus.Queued,
                QueuedAt = DateTimeOffset.UtcNow,
            }));
        _tracker = new JobRunTracker();
        _settings = Substitute.For<ISettingsSnapshot<LibrarySettings>>();
        _settings.Current.Returns(new LibrarySettings());
        _services = new ServiceCollection().AddScoped(_ => _jobs).BuildServiceProvider();
        _watcher = new MediaFolderWatcher(
            Microsoft.Extensions.Options.Options.Create(new MediaLibraryOptions { RootPath = _media.Path }),
            Microsoft.Extensions.Options.Options.Create(new ScanOptions
            {
                WatcherDebounce = TimeSpan.FromMilliseconds(300),
                WatcherMaxDelay = TimeSpan.FromSeconds(3),
            }),
            _settings,
            _services.GetRequiredService<IServiceScopeFactory>(),
            _tracker,
            TimeProvider.System,
            NullLogger<MediaFolderWatcher>.Instance);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _watcher.StopAsync(CancellationToken.None);
        _watcher.Dispose();
        await _services.DisposeAsync();
        _media.Dispose();
    }

    [Test]
    public async Task NewMediaFile_StartsOneScanOfItsFolder()
    {
        await _watcher.StartAsync(CancellationToken.None);

        await File.WriteAllTextAsync(_media.Combine("Artist/Album/new.mp3"), "audio");
        JobRunRequest request = await WaitForStartAsync();
        await Task.Delay(TimeSpan.FromSeconds(1));

        request.Trigger.Should().Be(JobRunTrigger.FileWatcher);
        JsonSerializer.Deserialize<string[]>(request.Payload!).Should().Equal("Artist/Album");
        StartedRequests().Should().ContainSingle();
    }

    [Test]
    public async Task SeveralChanges_AreScannedTogether()
    {
        await _watcher.StartAsync(CancellationToken.None);

        await File.WriteAllTextAsync(_media.Combine("Artist/Album/one.mp3"), "audio");
        await File.WriteAllTextAsync(_media.Combine("Artist/Album/two.flac"), "audio");
        await File.WriteAllTextAsync(_media.Combine("Other/cover.jpg"), "picture");
        await File.WriteAllTextAsync(_media.Combine("Other/notes.txt"), "ignored");
        JobRunRequest request = await WaitForStartAsync();

        JsonSerializer.Deserialize<string[]>(request.Payload!).Should().BeEquivalentTo("Artist/Album", "Other");
        StartedRequests().Should().ContainSingle();
    }

    [Test]
    public async Task OtherFiles_StartNoScan()
    {
        await _watcher.StartAsync(CancellationToken.None);

        await File.WriteAllTextAsync(_media.Combine("Other/notes.txt"), "ignored");
        _media.CreateDirectory(".hidden");
        await File.WriteAllTextAsync(_media.Combine(".hidden/song.mp3"), "audio");
        await Task.Delay(TimeSpan.FromSeconds(2));

        StartedRequests().Should().BeEmpty();
    }

    [Test]
    public async Task DisabledInTheSettings_StartsNoScan()
    {
        _settings.Current.Returns(new LibrarySettings { FileWatcherEnabled = false });
        await _watcher.StartAsync(CancellationToken.None);

        await File.WriteAllTextAsync(_media.Combine("Artist/Album/new.mp3"), "audio");
        await Task.Delay(TimeSpan.FromSeconds(2));

        _watcher.IsWatching.Should().BeFalse();
        StartedRequests().Should().BeEmpty();
    }

    [Test]
    public async Task SettingsChange_SwitchesTheWatcherOnAndOff()
    {
        await _watcher.StartAsync(CancellationToken.None);
        LibrarySettings on = new();
        LibrarySettings off = new() { FileWatcherEnabled = false };

        await _watcher.OnSettingsChangedAsync(on, off, CancellationToken.None);
        bool afterOff = _watcher.IsWatching;
        await _watcher.OnSettingsChangedAsync(off, on, CancellationToken.None);

        afterOff.Should().BeFalse();
        _watcher.IsWatching.Should().BeTrue();
    }

    [Test]
    public async Task WhileAScanRuns_KeepsTheChangesForLater()
    {
        var running = Guid.NewGuid();
        _tracker.MarkRunning(JobIds.LibraryScan, running, "fire");
        await _watcher.StartAsync(CancellationToken.None);

        await File.WriteAllTextAsync(_media.Combine("Other/later.mp3"), "audio");
        await Task.Delay(TimeSpan.FromSeconds(2));
        int whileRunning = StartedRequests().Count;
        _tracker.Release(JobIds.LibraryScan, running);
        JobRunRequest request = await WaitForStartAsync();

        whileRunning.Should().Be(0);
        JsonSerializer.Deserialize<string[]>(request.Payload!).Should().Equal("Other");
    }

    private List<JobRunRequest> StartedRequests()
    {
        return [.. _jobs.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IJobService.StartRunAsync))
            .Select(call => (JobRunRequest)call.GetArguments()[0]!)];
    }

    private async Task<JobRunRequest> WaitForStartAsync()
    {
        using CancellationTokenSource timeout = new(_wait);
        while (StartedRequests().Count == 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50), timeout.Token);
        }

        return StartedRequests()[0];
    }
}
