using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WaveMP.Core.Jobs.Definitions;
using WaveMP.Core.Jobs.Runs;
using WaveMP.Core.Jobs.Services;
using WaveMP.Core.Scanning.Discoveries;
using WaveMP.Core.Scanning.Jobs;
using WaveMP.Core.Scanning.Options;
using WaveMP.Core.Scanning.Plans;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Handlers;
using WaveMP.Core.Settings.Snapshots;
using WaveMP.Data.Entities;

namespace WaveMP.Core.Scanning.Watchers;

/// <summary>
/// Watches the media folder for new, changed and deleted media files and scans the changed folders shortly after
/// (debounced). File system events are unreliable on network shares, so the periodic scan always stays active. While
/// a scan runs, the changes are kept and scanned afterwards. Switched on and off by the library settings.
/// </summary>
public sealed class MediaFolderWatcher(
    IOptions<MediaLibraryOptions> mediaLibrary,
    IOptions<ScanOptions> options,
    ISettingsSnapshot<LibrarySettings> settings,
    IServiceScopeFactory scopeFactory,
    IJobRunTracker tracker,
    TimeProvider timeProvider,
    ILogger<MediaFolderWatcher> logger) : IHostedService, ISettingsChangeHandler<LibrarySettings>, IDisposable
{
    private readonly Lock _lock = new();
    private readonly MediaChangeBatch _batch = new();
    private readonly CancellationTokenSource _stopping = new();
    private FileSystemWatcher? _watcher;
    private Task? _loop;

    /// <summary>
    /// Whether the watcher is watching the media folder.
    /// </summary>
    public bool IsWatching
    {
        get
        {
            lock (_lock)
            {
                return _watcher is not null;
            }
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (settings.Current.FileWatcherEnabled)
        {
            Enable();
        }

        _loop = Task.Run(() => RunAsync(_stopping.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Disable();
        await _stopping.CancelAsync();
        if (_loop is not null)
        {
            await _loop.WaitAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }

    public ValueTask OnSettingsChangedAsync(
        LibrarySettings previous,
        LibrarySettings current,
        CancellationToken cancellationToken)
    {
        if (current.FileWatcherEnabled && !IsWatching)
        {
            Enable();
        }
        else if (!current.FileWatcherEnabled && IsWatching)
        {
            Disable();
        }

        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        Disable();
        _stopping.Dispose();
    }

    private void Enable()
    {
        string root = mediaLibrary.Value.RootPath;
        if (!Directory.Exists(root))
        {
            logger.LogWarning("The media folder {MediaFolder} does not exist; the file watcher is off", root);
            return;
        }

        try
        {
            FileSystemWatcher watcher = new(root)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName
                    | NotifyFilters.DirectoryName
                    | NotifyFilters.LastWrite
                    | NotifyFilters.Size,
                InternalBufferSize = 64 * 1024,
            };
            watcher.Created += (_, change) => OnChanged(change.FullPath, change.ChangeType);
            watcher.Changed += (_, change) => OnChanged(change.FullPath, change.ChangeType);
            watcher.Deleted += (_, change) => OnChanged(change.FullPath, change.ChangeType);
            watcher.Renamed += (_, change) =>
            {
                OnChanged(change.OldFullPath, WatcherChangeTypes.Deleted);
                OnChanged(change.FullPath, WatcherChangeTypes.Created);
            };
            watcher.Error += (_, error) => OnError(error.GetException());
            watcher.EnableRaisingEvents = true;

            lock (_lock)
            {
                _watcher?.Dispose();
                _watcher = watcher;
            }

            logger.LogInformation("Watching the media folder {MediaFolder} for changes", root);
        }
        catch (Exception exception) when (exception is IOException
            or ArgumentException
            or UnauthorizedAccessException
            or PlatformNotSupportedException)
        {
            // E.g. the inotify limits of Linux; the periodic scan still finds the changes.
            logger.LogWarning(exception, "The file watcher cannot watch {MediaFolder}; it is off", root);
        }
    }

    private void Disable()
    {
        lock (_lock)
        {
            _watcher?.Dispose();
            _watcher = null;
            _batch.Clear();
        }
    }

    private void OnChanged(string fullPath, WatcherChangeTypes change)
    {
        string root = mediaLibrary.Value.RootPath;
        string relative = ScanScope.Normalize(Path.GetRelativePath(root, fullPath));
        if (relative.StartsWith("..", StringComparison.Ordinal) || IgnoredNames.IsIgnoredPath(relative))
        {
            return;
        }

        string name = Path.GetFileName(fullPath);
        bool isMedia = new MediaKindClassifier(settings.Current).Classify(name) is not null;

        // Folders: created or deleted ones change their parent; changes of their content are reported for the files.
        bool isFolder = !isMedia && (change == WatcherChangeTypes.Deleted || Directory.Exists(fullPath));
        if (!isMedia && (!isFolder || change == WatcherChangeTypes.Changed))
        {
            return;
        }

        int slash = relative.LastIndexOf('/');
        lock (_lock)
        {
            if (_watcher is not null)
            {
                _batch.Add(slash < 0 ? string.Empty : relative[..slash], timeProvider.GetUtcNow());
            }
        }
    }

    private void OnError(Exception exception)
    {
        logger.LogWarning(exception, "The file watcher missed changes; the whole media folder is scanned");
        lock (_lock)
        {
            if (_watcher is not null)
            {
                _batch.RequestFull(timeProvider.GetUtcNow());
            }
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        ScanOptions scanOptions = options.Value;
        var interval = TimeSpan.FromMilliseconds(
            Math.Clamp(scanOptions.WatcherDebounce.TotalMilliseconds / 4, 20, 1000));
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, timeProvider, cancellationToken);
                await StartDueScanAsync(scanOptions, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "The file watcher could not start a scan");
            }
        }
    }

    private async Task StartDueScanAsync(ScanOptions scanOptions, CancellationToken cancellationToken)
    {
        ScanScope scope;
        DateTimeOffset firstChange;
        lock (_lock)
        {
            if (!_batch.IsDue(timeProvider.GetUtcNow(), scanOptions.WatcherDebounce, scanOptions.WatcherMaxDelay)
                || tracker.GetState(JobIds.LibraryScan) != JobActivityState.Idle)
            {
                // A running scan does not see every change; the changes wait for the next scan.
                return;
            }

            firstChange = _batch.FirstChange!.Value;
            scope = _batch.Take();
        }

        JobRunStartResult result;
        try
        {
            await using AsyncServiceScope serviceScope = scopeFactory.CreateAsyncScope();
            IJobService jobs = serviceScope.ServiceProvider.GetRequiredService<IJobService>();
            result = await jobs.StartRunAsync(
                new JobRunRequest(
                    JobIds.LibraryScan,
                    JobRunTrigger.FileWatcher,
                    scope.IsFull ? null : LibraryScanJob.CreatePayload(scope)),
                cancellationToken);
        }
        catch
        {
            Restore(scope, firstChange);
            throw;
        }

        if (!result.IsSuccess)
        {
            Restore(scope, firstChange);
            return;
        }

        logger.LogInformation(
            "Changes in {Folders} found by the file watcher, scan {RunId} started",
            scope.IsFull ? "the media folder" : string.Join(", ", scope.Directories),
            result.Run.Id);
    }

    private void Restore(ScanScope scope, DateTimeOffset firstChange)
    {
        lock (_lock)
        {
            if (_watcher is not null)
            {
                _batch.Restore(scope, firstChange);
            }
        }
    }
}
