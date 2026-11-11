using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WaveMP.Core.Jobs.Runs;
using WaveMP.Core.Metadata.Probes;
using WaveMP.Core.Metadata.Readers;
using WaveMP.Core.Scanning.Discoveries;
using WaveMP.Core.Scanning.Options;
using WaveMP.Core.Scanning.Plans;
using WaveMP.Core.Scanning.Writers;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Snapshots;
using WaveMP.Data.Entities;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Scanning.Scans;

/// <summary>
/// Scans the media folder: checks the folder and ffprobe, walks the folders of the scope, compares them with the
/// library, reads new and changed files in parallel, writes every changed folder at once, removes folders that are
/// gone and finally updates album covers, removes artists and albums without tracks and counts the library.
/// A full scan that finds no files at all while the library has files fails and changes nothing, so an unmounted
/// media folder never empties the library.
/// </summary>
internal sealed class LibraryScanner(
    ILibraryScanRepository library,
    LibraryWriter writer,
    IMediaMetadataReader reader,
    IMediaProber prober,
    ISettingsSnapshot<LibrarySettings> settings,
    IScanProgressTracker progress,
    IOptions<MediaLibraryOptions> mediaLibrary,
    IOptions<ScanOptions> options,
    TimeProvider timeProvider,
    ILogger<LibraryScanner> logger) : ILibraryScanner
{
    public async Task<ScanResult> ScanAsync(
        ScanScope scope,
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        progress.Start(runId);
        try
        {
            return await RunAsync(scope, cancellationToken);
        }
        finally
        {
            progress.Complete(runId);
        }
    }

    private async Task<ScanResult> RunAsync(ScanScope scope, CancellationToken cancellationToken)
    {
        string root = mediaLibrary.Value.RootPath;
        ScanOptions scanOptions = options.Value;
        LibrarySettings librarySettings = settings.Current;
        IssueCollector issues = new(scanOptions.MaxIssuesPerScan);

        if (!await prober.IsAvailableAsync(cancellationToken))
        {
            throw new JobFailedException(JobErrorCode.ProbeUnavailable, "ffprobe cannot be started.");
        }

        CheckMediaFolder(root);
        logger.LogInformation(
            "Scanning {Scope} of the media folder {MediaFolder}",
            scope.IsFull ? "all folders" : string.Join(", ", scope.Directories),
            root);

        MediaKindClassifier classifier = new(librarySettings);
        List<DiscoveredDirectory> discovered = [];
        foreach (string directory in scope.Directories)
        {
            discovered.AddRange(MediaFolderWalker.Walk(
                root,
                directory,
                classifier,
                scanOptions.MaxPathBytes,
                path => issues.Add(path, ScanIssueCode.PathTooLong),
                cancellationToken));
            progress.Discovered(discovered.Count, discovered.Sum(item => item.Files.Count));
        }

        IReadOnlyList<MediaFileSnapshot> stored = await library.GetFileSnapshotsAsync(
            scope.IsFull ? null : scope.Directories,
            cancellationToken);
        ScanPlan plan = ScanPlanner.Plan(discovered, stored, ScannerVersions.Metadata);
        if (scope.IsFull && plan.FilesFound == 0 && stored.Count > 0)
        {
            throw new JobFailedException(
                JobErrorCode.MediaFolderEmpty,
                $"The media folder {root} has no media files, but the library has {stored.Count}. Nothing was "
                + "removed; check that the media folder is mounted.");
        }

        progress.Planned(
            plan.ChangedDirectories.Count,
            plan.ChangedDirectories.Sum(directory => directory.FilesToRead.Count));

        int tracksAdded = 0;
        int tracksUpdated = 0;
        int tracksRemoved = 0;
        foreach (DirectoryPlan directory in plan.ChangedDirectories)
        {
            IReadOnlyDictionary<string, MediaFileMetadata> read = await ReadAsync(
                root,
                directory.FilesToRead,
                scanOptions.MetadataParallelism,
                issues,
                cancellationToken);
            FolderWriteResult written = await writer.WriteFolderAsync(
                directory,
                read,
                timeProvider.GetUtcNow(),
                cancellationToken);
            tracksAdded += written.TracksAdded;
            tracksUpdated += written.TracksUpdated;
            tracksRemoved += written.TracksRemoved;
            progress.DirectoryProcessed();
        }

        progress.CleaningUp();
        (int _, int deletedTracks) = await library.DeleteDirectoriesAsync(plan.DeletedDirectories, cancellationToken);
        tracksRemoved += deletedTracks;

        bool changed = plan.ChangedDirectories.Count > 0 || plan.DeletedDirectories.Count > 0;
        if (changed)
        {
            await library.DeleteOrphansAsync(cancellationToken);
        }

        // A full scan always updates the album covers, so changed cover names apply without changed files.
        if (changed || scope.IsFull)
        {
            await writer.UpdateAlbumCoversAsync(librarySettings.AlbumCoverNames, cancellationToken);
        }

        LibraryTotals totals = await library.CountLibraryAsync(cancellationToken);
        ScanStatistics statistics = new()
        {
            DirectoriesScanned = plan.DirectoriesFound,
            DirectoriesChanged = plan.ChangedDirectories.Count + plan.DeletedDirectories.Count,
            FilesFound = plan.FilesFound,
            FilesAdded = plan.ChangedDirectories.Sum(directory => directory.Added),
            FilesUpdated = plan.ChangedDirectories.Sum(directory => directory.Updated),
            FilesRemoved = plan.ChangedDirectories.Sum(directory => directory.Removed) + plan.FilesInDeletedDirectories,
            FilesUnchanged = plan.FilesUnchanged,
            FilesFailed = issues.Count,
            TracksAdded = tracksAdded,
            TracksUpdated = tracksUpdated,
            TracksRemoved = tracksRemoved,
            TracksTotal = totals.Tracks,
            AlbumsTotal = totals.Albums,
            ArtistsTotal = totals.Artists,
            IsPartial = !scope.IsFull,
        };

        logger.LogInformation(
            "Scan finished: {FilesFound} files, {FilesAdded} added, {FilesUpdated} updated, {FilesRemoved} removed, "
            + "{FilesFailed} with problems; {TracksTotal} tracks",
            statistics.FilesFound,
            statistics.FilesAdded,
            statistics.FilesUpdated,
            statistics.FilesRemoved,
            statistics.FilesFailed,
            statistics.TracksTotal);
        return new ScanResult(statistics, issues.Listed);
    }

    private static void CheckMediaFolder(string root)
    {
        if (!Directory.Exists(root))
        {
            throw new JobFailedException(
                JobErrorCode.MediaFolderNotFound,
                $"The media folder {root} does not exist.");
        }

        try
        {
            using IEnumerator<string> entries = Directory.EnumerateFileSystemEntries(root).GetEnumerator();
            entries.MoveNext();
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException)
        {
            throw new JobFailedException(
                JobErrorCode.MediaFolderUnreadable,
                $"The media folder {root} cannot be read.",
                exception);
        }
    }

    private async Task<IReadOnlyDictionary<string, MediaFileMetadata>> ReadAsync(
        string root,
        IReadOnlyList<DiscoveredFile> files,
        int parallelism,
        IssueCollector issues,
        CancellationToken cancellationToken)
    {
        ConcurrentDictionary<string, MediaFileMetadata> read = new(StringComparer.Ordinal);
        ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = Math.Max(1, parallelism),
            CancellationToken = cancellationToken,
        };

        try
        {
            await Parallel.ForEachAsync(files, parallelOptions, async (file, token) =>
            {
                string path = Path.Combine(root, MediaFolderWalker.ToSystemPath(file.RelativePath));
                MediaFileMetadata metadata = await reader.ReadAsync(path, file.Kind, token);
                if (metadata.Issue is { } issue)
                {
                    issues.Add(file.RelativePath, issue switch
                    {
                        MediaFileIssue.TagsUnreadable => ScanIssueCode.TagsUnreadable,
                        MediaFileIssue.ProbeFailed => ScanIssueCode.ProbeFailed,
                        _ => ScanIssueCode.FileUnreadable,
                    });
                }

                read[file.RelativePath] = metadata;
                progress.FileRead();
            });
        }
        catch (ProbeUnavailableException exception)
        {
            throw new JobFailedException(JobErrorCode.ProbeUnavailable, exception.Message, exception);
        }

        return read;
    }

    /// <summary>
    /// Counts all problems and keeps the first ones for the result.
    /// </summary>
    private sealed class IssueCollector(int maxListed)
    {
        private readonly Lock _lock = new();
        private readonly List<ScanIssue> _listed = [];

        public int Count { get; private set; }

        public IReadOnlyList<ScanIssue> Listed
        {
            get
            {
                lock (_lock)
                {
                    return [.. _listed.OrderBy(issue => issue.Path, StringComparer.Ordinal)];
                }
            }
        }

        public void Add(string path, ScanIssueCode code)
        {
            lock (_lock)
            {
                Count++;
                if (_listed.Count < maxListed)
                {
                    _listed.Add(new ScanIssue(path, code));
                }
            }
        }
    }
}
