using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WaveMP.Core.Caching.Options;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Snapshots;

namespace WaveMP.Core.Caching.Stores;

internal sealed class CacheStore(
    IOptions<CacheOptions> options,
    ISettingsSnapshot<CacheSettings> settings,
    TimeProvider timeProvider,
    ILogger<CacheStore> logger) : ICacheStore
{
    private const int BufferSize = 64 * 1024;

    private static readonly EnumerationOptions _allFiles = new()
    {
        RecurseSubdirectories = true,
        IgnoreInaccessible = true,
        AttributesToSkip = 0,
    };

    private readonly Lock _lock = new();
    private readonly Dictionary<(CacheArea Area, string Name), CacheIndexEntry> _entries = [];
    private readonly long[] _sizes = new long[CacheFolders.Areas.Count];
    private readonly int[] _counts = new int[CacheFolders.Areas.Count];

    // Serializes the clean-up, removals and clearing; reads and new files never wait for it.
    private readonly SemaphoreSlim _maintenance = new(1, 1);
    private int _trimScheduled;
    private Task _trimTask = Task.CompletedTask;
    private volatile bool _available;

    public bool IsAvailable => _available;

    public string RootPath => options.Value.RootPath;

    /// <summary>
    /// The clean-up started last by <see cref="ScheduleTrim"/>, for tests.
    /// </summary>
    internal Task CurrentTrim => Volatile.Read(ref _trimTask);

    private string TemporaryPath => Path.Combine(RootPath, CacheFolders.Temporary);

    public string GetAreaPath(CacheArea area)
    {
        return Path.Combine(RootPath, CacheFolders.FolderOf(area));
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(Initialize, cancellationToken);
    }

    public Stream? TryOpen(CacheArea area, string name)
    {
        if (!_available || !CacheFolders.TryGetPath(GetAreaPath(area), name, out string path))
        {
            return null;
        }

        CacheIndexEntry? entry;
        lock (_lock)
        {
            if (!_entries.TryGetValue((area, name), out entry))
            {
                return null;
            }
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        bool persisted = now - entry.PersistedUseAt >= options.Value.UsageTouchInterval && TryTouch(path, now);

        FileStream stream;
        try
        {
            stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read | FileShare.Delete,
                BufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            // Removed outside of WaveMP.
            Forget(area, name);
            return null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogDebug(exception, "Cannot read {Name} from the cache", name);
            return null;
        }

        lock (_lock)
        {
            if (_entries.TryGetValue((area, name), out CacheIndexEntry? current))
            {
                _entries[(area, name)] = current with
                {
                    LastUsedAt = now,
                    PersistedUseAt = persisted ? now : current.PersistedUseAt,
                };
            }
        }

        return stream;
    }

    public bool Contains(CacheArea area, string name)
    {
        if (!_available || !CacheFolders.IsValidName(name))
        {
            return false;
        }

        lock (_lock)
        {
            return _entries.ContainsKey((area, name));
        }
    }

    public string CreateTemporaryPath(string extension)
    {
        string suffix = extension.StartsWith('.') ? extension : "." + extension;
        return Path.Combine(TemporaryPath, Guid.NewGuid().ToString("N") + suffix);
    }

    public bool Commit(CacheArea area, string name, string temporaryPath)
    {
        if (!_available || !CacheFolders.TryGetPath(GetAreaPath(area), name, out string path))
        {
            DeleteTemporary(temporaryPath);
            return false;
        }

        long size;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.Move(temporaryPath, path, overwrite: true);
            size = new FileInfo(path).Length;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Cannot store {Name} in the cache", name);
            DeleteTemporary(temporaryPath);
            return false;
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        lock (_lock)
        {
            Set(new CacheIndexEntry(area, name, size, now, now));
        }

        if (IsOverLimit())
        {
            ScheduleTrim();
        }

        return true;
    }

    public void DeleteTemporary(string temporaryPath)
    {
        try
        {
            File.Delete(temporaryPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogDebug(exception, "Cannot remove the temporary file {Path}", temporaryPath);
        }
    }

    public CacheUsage GetUsage()
    {
        long maxSize = settings.Current.GetMaxSizeBytes();
        if (!_available)
        {
            return new CacheUsage(false, 0, 0, maxSize, CacheAreaUsage.Empty, CacheAreaUsage.Empty);
        }

        lock (_lock)
        {
            return new CacheUsage(
                true,
                _sizes.Sum(),
                _counts.Sum(),
                maxSize,
                new CacheAreaUsage(_sizes[(int)CacheArea.Covers], _counts[(int)CacheArea.Covers]),
                new CacheAreaUsage(_sizes[(int)CacheArea.Transcodes], _counts[(int)CacheArea.Transcodes]));
        }
    }

    public async Task<int> RemoveAsync(
        CacheArea area,
        Func<string, bool> predicate,
        CancellationToken cancellationToken = default)
    {
        if (!_available)
        {
            return 0;
        }

        await _maintenance.WaitAsync(cancellationToken);
        try
        {
            CacheIndexEntry[] matching;
            lock (_lock)
            {
                matching = [.. _entries.Values.Where(entry => entry.Area == area && predicate(entry.Name))];
            }

            return DeleteAll(matching);
        }
        finally
        {
            _maintenance.Release();
        }
    }

    public async Task<int> TrimAsync(CancellationToken cancellationToken = default)
    {
        if (!_available)
        {
            return 0;
        }

        await _maintenance.WaitAsync(cancellationToken);
        try
        {
            CacheIndexEntry[] entries;
            long total;
            lock (_lock)
            {
                entries = [.. _entries.Values];
                total = _sizes.Sum();
            }

            long maxSize = settings.Current.GetMaxSizeBytes();
            IReadOnlyList<CacheIndexEntry> planned =
                LruTrimPlanner.Plan(entries, total, maxSize, options.Value.TrimTargetRatio);
            if (planned.Count == 0)
            {
                return 0;
            }

            int removed = DeleteAll(planned);
            logger.LogInformation(
                "Removed {Count} files used least recently from the cache, which was larger than {MaxSize} bytes",
                removed,
                maxSize);
            return removed;
        }
        finally
        {
            _maintenance.Release();
        }
    }

    public void ScheduleTrim()
    {
        if (!_available || Interlocked.CompareExchange(ref _trimScheduled, 1, 0) != 0)
        {
            return;
        }

        Volatile.Write(ref _trimTask, Task.Run(async () =>
        {
            try
            {
                await TrimAsync();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "The clean-up of the cache failed");
            }
            finally
            {
                Volatile.Write(ref _trimScheduled, 0);
            }
        }));
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        if (!_available)
        {
            return;
        }

        await _maintenance.WaitAsync(cancellationToken);
        try
        {
            foreach (CacheArea area in CacheFolders.Areas)
            {
                ClearArea(area);
            }

            logger.LogInformation("The cache was cleared");
        }
        finally
        {
            _maintenance.Release();
        }
    }

    private void Initialize()
    {
        try
        {
            foreach (CacheArea area in CacheFolders.Areas)
            {
                Directory.CreateDirectory(GetAreaPath(area));
            }

            Directory.CreateDirectory(TemporaryPath);
            string probe = Path.Combine(TemporaryPath, $"probe-{Guid.NewGuid():N}");
            File.WriteAllBytes(probe, []);
            File.Delete(probe);
            ClearTemporary();

            lock (_lock)
            {
                _entries.Clear();
                Array.Clear(_sizes);
                Array.Clear(_counts);
                foreach (CacheArea area in CacheFolders.Areas)
                {
                    foreach (CacheIndexEntry entry in Scan(area))
                    {
                        Set(entry);
                    }
                }
            }

            _available = true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _available = false;
            logger.LogError(
                exception,
                "The cache folder {CacheFolder} cannot be written; covers are delivered unscaled. Check the volume "
                + "of DATA_PATH",
                RootPath);
            return;
        }

        CacheUsage usage = GetUsage();
        logger.LogInformation(
            "The cache {CacheFolder} holds {Count} files with {Size} bytes",
            RootPath,
            usage.EntryCount,
            usage.SizeBytes);
        if (IsOverLimit())
        {
            ScheduleTrim();
        }
    }

    private void ClearTemporary()
    {
        DirectoryInfo directory = new(TemporaryPath);
        foreach (FileSystemInfo item in directory.EnumerateFileSystemInfos())
        {
            try
            {
                if (item is DirectoryInfo folder)
                {
                    folder.Delete(recursive: true);
                }
                else
                {
                    item.Delete();
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                logger.LogDebug(exception, "Cannot remove the unfinished file {Path}", item.FullName);
            }
        }
    }

    private void ClearArea(CacheArea area)
    {
        try
        {
            string areaPath = GetAreaPath(area);
            Directory.CreateDirectory(areaPath);
            foreach (string file in Directory.EnumerateFiles(areaPath, "*", _allFiles))
            {
                TryDelete(file);
            }

            // Files that could not be removed stay known; the empty folders stay as well, new files may be written to
            // them at this moment.
            lock (_lock)
            {
                foreach ((CacheArea, string) key in _entries.Keys.Where(key => key.Area == area).ToArray())
                {
                    _entries.Remove(key);
                }

                _sizes[(int)area] = 0;
                _counts[(int)area] = 0;
                foreach (CacheIndexEntry entry in Scan(area))
                {
                    Set(entry);
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Cannot clear the area {Area} of the cache", area);
        }
    }

    private IEnumerable<CacheIndexEntry> Scan(CacheArea area)
    {
        string areaPath = GetAreaPath(area);
        foreach (FileInfo file in new DirectoryInfo(areaPath).EnumerateFiles("*", _allFiles))
        {
            DateTimeOffset usedAt = new(file.LastWriteTimeUtc, TimeSpan.Zero);
            yield return new CacheIndexEntry(
                area,
                CacheFolders.NameOf(areaPath, file.FullName),
                file.Length,
                usedAt,
                usedAt);
        }
    }

    private int DeleteAll(IEnumerable<CacheIndexEntry> entries)
    {
        int removed = 0;
        foreach (CacheIndexEntry entry in entries)
        {
            if (TryDelete(FullPathOf(entry.Area, entry.Name)))
            {
                Forget(entry.Area, entry.Name);
                removed++;
            }
        }

        return removed;
    }

    private bool TryDelete(string path)
    {
        try
        {
            File.Delete(path);
            return true;
        }
        catch (DirectoryNotFoundException)
        {
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogDebug(exception, "Cannot remove {Path} from the cache; it is skipped", path);
            return false;
        }
    }

    /// <summary>
    /// Writes the use as time of the last change of the file (best effort).
    /// </summary>
    private bool TryTouch(string path, DateTimeOffset usedAt)
    {
        try
        {
            File.SetLastWriteTimeUtc(path, usedAt.UtcDateTime);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogDebug(exception, "Cannot record the use of {Path}", path);
            return false;
        }
    }

    private string FullPathOf(CacheArea area, string name)
    {
        return Path.Combine([GetAreaPath(area), .. name.Split('/')]);
    }

    private bool IsOverLimit()
    {
        long maxSize = settings.Current.GetMaxSizeBytes();
        lock (_lock)
        {
            return _sizes.Sum() > maxSize;
        }
    }

    /// <summary>
    /// Adds or replaces an entry and updates the sums; the caller holds the lock.
    /// </summary>
    private void Set(CacheIndexEntry entry)
    {
        if (_entries.TryGetValue((entry.Area, entry.Name), out CacheIndexEntry? previous))
        {
            _sizes[(int)previous.Area] -= previous.Size;
            _counts[(int)previous.Area]--;
        }

        _entries[(entry.Area, entry.Name)] = entry;
        _sizes[(int)entry.Area] += entry.Size;
        _counts[(int)entry.Area]++;
    }

    private void Forget(CacheArea area, string name)
    {
        lock (_lock)
        {
            if (_entries.Remove((area, name), out CacheIndexEntry? entry))
            {
                _sizes[(int)area] -= entry.Size;
                _counts[(int)area]--;
            }
        }
    }
}
