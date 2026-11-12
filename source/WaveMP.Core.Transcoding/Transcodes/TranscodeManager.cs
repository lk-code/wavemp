using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WaveMP.Core.Caching.Stores;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Snapshots;
using WaveMP.Core.Transcoding.Accelerators;
using WaveMP.Core.Transcoding.Arguments;
using WaveMP.Core.Transcoding.Options;
using WaveMP.Core.Transcoding.Playlists;
using WaveMP.Core.Transcoding.Processes;
using WaveMP.Core.Transcoding.Profiles;

namespace WaveMP.Core.Transcoding.Transcodes;

internal sealed class TranscodeManager(
    IFfmpegProcessFactory processes,
    ICacheStore cache,
    IHardwareAccelerationService hardware,
    ISettingsSnapshot<TranscodingSettings> settings,
    IOptions<TranscodingOptions> options,
    TimeProvider timeProvider,
    ILogger<TranscodeManager> logger) : ITranscodeManager, IDisposable
{
    /// <summary>
    /// A transcode that fails with the hardware before its first segment is tried once more in software.
    /// </summary>
    private const int MaxAttempts = 2;

    private static readonly TimeSpan _shutdownWait = TimeSpan.FromSeconds(5);

    private readonly Lock _lock = new();
    private readonly List<TranscodeJob> _jobs = [];
    private bool _disposed;

    public async Task<SegmentResult> GetSegmentAsync(
        TranscodeVariant variant,
        int index,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        if (index < 0 || index >= variant.SegmentCount)
        {
            return SegmentResult.Of(SegmentStatus.NotFound);
        }

        string name = variant.SegmentNameOf(index);
        for (int attempt = 1; ; attempt++)
        {
            if (cache.TryOpen(CacheArea.Transcodes, name) is { } cached)
            {
                return SegmentResult.Ready(cached);
            }

            if (!cache.IsAvailable)
            {
                return SegmentResult.Of(SegmentStatus.Unavailable);
            }

            (TranscodeJob? job, SegmentStatus status) = Acquire(variant, index, userId);
            if (job is null)
            {
                return SegmentResult.Of(status);
            }

            SegmentWait wait = await job.WaitForSegmentAsync(index, options.Value.SegmentWaitTimeout, cancellationToken);
            if (cache.TryOpen(CacheArea.Transcodes, name) is { } produced)
            {
                return SegmentResult.Ready(produced);
            }

            switch (wait)
            {
                case SegmentWait.TimedOut:
                    return SegmentResult.Of(SegmentStatus.Timeout);
                case SegmentWait.Ready:
                    // Finished, but it could not be put into the cache.
                    return SegmentResult.Of(SegmentStatus.Failed);
            }

            switch (job.State)
            {
                case TranscodeJobState.Failed
                    when job.Accelerator != HardwareAccelerator.None && !job.ProducedAny && attempt < MaxAttempts:
                    hardware.ReportEncodeFailure(job.Accelerator);
                    continue;
                case TranscodeJobState.Failed:
                    return SegmentResult.Of(SegmentStatus.Failed);
                case TranscodeJobState.Cancelled:
                    // Stopped for another transcode (e.g. a seek of another player); the client tries again.
                    return SegmentResult.Of(SegmentStatus.Busy);
                default:
                    // ffmpeg finished without this segment: the file is shorter than its stored duration.
                    return SegmentResult.Of(SegmentStatus.NotFound);
            }
        }
    }

    public IReadOnlyList<TranscodeJobInfo> ListJobs()
    {
        lock (_lock)
        {
            return [.. _jobs.Where(job => job.IsActive)
                .OrderByDescending(job => job.StartedAt)
                .Select(job => job.ToInfo())];
        }
    }

    public bool Cancel(Guid id)
    {
        lock (_lock)
        {
            TranscodeJob? job = _jobs.FirstOrDefault(candidate => candidate.Id == id && candidate.IsActive);
            if (job is null)
            {
                return false;
            }

            logger.LogInformation("Transcode {JobId} of track {TrackId} cancelled", job.Id, job.Variant.Input.TrackId);
            job.Stop();
            return true;
        }
    }

    public int StopIdle()
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        TimeSpan idleTimeout = options.Value.IdleTimeout;
        lock (_lock)
        {
            List<TranscodeJob> idle = [.. _jobs.Where(job => job.IsActive && now - job.LastAccess >= idleTimeout)];
            foreach (TranscodeJob job in idle)
            {
                logger.LogDebug("Stopping idle transcode {JobId}", job.Id);
                job.Stop();
            }

            return idle.Count;
        }
    }

    public void Dispose()
    {
        Task[] watchers;
        lock (_lock)
        {
            _disposed = true;
            foreach (TranscodeJob job in _jobs.Where(job => !job.HasEnded))
            {
                job.Stop();
            }

            watchers = [.. _jobs.Select(job => job.Watcher)];
        }

        try
        {
            Task.WaitAll(watchers, _shutdownWait);
        }
        catch (AggregateException)
        {
            // The watchers log their own errors.
        }
    }

    private (TranscodeJob? Job, SegmentStatus Status) Acquire(TranscodeVariant variant, int index, Guid? userId)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        TranscodingOptions technical = options.Value;
        int ahead = (int)Math.Ceiling(technical.RestartAhead / variant.Profile.SegmentDuration);
        lock (_lock)
        {
            if (_disposed)
            {
                return (null, SegmentStatus.Unavailable);
            }

            List<TranscodeJob> active = [.. _jobs.Where(job => job.IsActive)];
            List<TranscodeJob> sameVariant = [.. active.Where(job => job.Key == variant.Id)];
            if (sameVariant.FirstOrDefault(job => job.Covers(index, ahead)) is { } running)
            {
                running.Touch(now);
                return (running, SegmentStatus.Ready);
            }

            // A seek: the transcode of the variant moves to the requested segment.
            foreach (TranscodeJob job in sameVariant)
            {
                logger.LogDebug("Restarting transcode {JobId} at segment {Index}", job.Id, index);
                job.Stop();
                active.Remove(job);
            }

            if (active.Count >= Math.Max(1, settings.Current.MaxParallelJobs))
            {
                TranscodeJob? idle = active
                    .Where(job => now - job.LastAccess >= technical.EvictIdleAfter)
                    .OrderBy(job => job.LastAccess)
                    .FirstOrDefault();
                if (idle is null)
                {
                    return (null, SegmentStatus.Busy);
                }

                logger.LogDebug("Transcode {JobId} gives its slot to a new one", idle.Id);
                idle.Stop();
            }

            return Start(variant, index, userId, now);
        }
    }

    private (TranscodeJob? Job, SegmentStatus Status) Start(
        TranscodeVariant variant,
        int index,
        Guid? userId,
        DateTimeOffset now)
    {
        HardwareAccelerationStatus status = hardware.Current;
        if (!status.FfmpegAvailable)
        {
            return (null, SegmentStatus.Unavailable);
        }

        HardwareAccelerator accelerator =
            variant.Media == HlsMedia.Video && settings.Current.HardwareAcceleration != HardwareAccelerationMode.Off
                ? status.EncoderOfNewJobs
                : HardwareAccelerator.None;

        string folder;
        try
        {
            folder = cache.CreateTemporaryDirectory();
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException
                                              or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "No temporary folder for a transcode");
            return (null, SegmentStatus.Unavailable);
        }

        IReadOnlyList<string> arguments = FfmpegHlsArguments.Build(
            variant,
            index,
            accelerator,
            status.Device,
            folder,
            options.Value.X264Preset);
        IFfmpegProcess process;
        try
        {
            process = processes.Start(arguments);
        }
        catch (FfmpegStartException exception)
        {
            logger.LogError(exception, "ffmpeg cannot be started for a transcode");
            cache.DeleteTemporaryDirectory(folder);
            return (null, SegmentStatus.Unavailable);
        }

        TranscodeJob job = new(Guid.NewGuid(), variant, index, accelerator, folder, process, userId, now);
        _jobs.Add(job);
        job.Watcher = Task.Run(() => WatchAsync(job));
        logger.LogInformation(
            "Transcode {JobId} of track {TrackId} ({Media}, {Accelerator}) started at segment {Index}",
            job.Id,
            variant.Input.TrackId,
            variant.Media,
            accelerator,
            index);
        return (job, SegmentStatus.Ready);
    }

    private async Task WatchAsync(TranscodeJob job)
    {
        TranscodeJobState state = TranscodeJobState.Failed;
        try
        {
            TimeSpan poll = options.Value.PollInterval;
            while (!job.Process.Exited.IsCompleted)
            {
                await Task.WhenAny(job.Process.Exited, Task.Delay(poll, timeProvider));
                Collect(job);
            }

            int exitCode = await job.Process.Exited;
            Collect(job);
            state = job.StopRequested
                ? TranscodeJobState.Cancelled
                : exitCode == 0
                    ? TranscodeJobState.Completed
                    : TranscodeJobState.Failed;
            if (state == TranscodeJobState.Failed)
            {
                logger.LogWarning(
                    "Transcode {JobId} of {Path} failed with exit code {ExitCode}: {Error}",
                    job.Id,
                    job.Variant.Input.FullPath,
                    exitCode,
                    job.Process.ErrorOutput);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Watching transcode {JobId} failed", job.Id);
            job.Process.Kill();
        }
        finally
        {
            lock (_lock)
            {
                _jobs.Remove(job);
            }

            job.Finish(state);
            job.Process.Dispose();
            cache.DeleteTemporaryDirectory(job.Folder);
        }
    }

    /// <summary>
    /// Moves the segments ffmpeg finished since the last look into the cache.
    /// </summary>
    private void Collect(TranscodeJob job)
    {
        string? playlist = ReadPlaylist(Path.Combine(job.Folder, FfmpegHlsArguments.PlaylistName));
        if (playlist is null)
        {
            return;
        }

        int count = job.Variant.SegmentCount;
        foreach (int index in HlsPlaylistWriter.ReadSegmentIndexes(playlist))
        {
            if (index < job.NextSegment || index >= count)
            {
                continue;
            }

            string path = Path.Combine(job.Folder, $"{FfmpegHlsArguments.SegmentPrefix}{index:D5}.ts");
            if (!File.Exists(path))
            {
                return;
            }

            if (!cache.Commit(CacheArea.Transcodes, job.Variant.SegmentNameOf(index), path))
            {
                logger.LogWarning("Segment {Index} of transcode {JobId} cannot be put into the cache", index, job.Id);
            }

            job.Advance(index + 1);
        }
    }

    private static string? ReadPlaylist(string path)
    {
        try
        {
            // ffmpeg replaces the playlist while it is read; sharing delete keeps the rename possible on Windows.
            using FileStream stream = new(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
