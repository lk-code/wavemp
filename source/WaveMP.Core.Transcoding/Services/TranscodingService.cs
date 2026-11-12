using Microsoft.Extensions.Options;
using WaveMP.Core.Caching.Stores;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Snapshots;
using WaveMP.Core.Streaming.Sources;
using WaveMP.Core.Transcoding.Accelerators;
using WaveMP.Core.Transcoding.Options;
using WaveMP.Core.Transcoding.Profiles;
using WaveMP.Core.Transcoding.Transcodes;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Transcoding.Services;

internal sealed class TranscodingService(
    IStreamService streams,
    ITranscodeManager manager,
    IHardwareAccelerationService hardware,
    ICacheStore cache,
    ITrackRepository tracks,
    ISettingsSnapshot<TranscodingSettings> settings,
    IOptions<TranscodingOptions> options) : ITranscodingService
{
    public bool IsAvailable => hardware.Current.FfmpegAvailable && cache.IsAvailable;

    public async Task<TranscodeVariantResult> ResolveAsync(
        Guid trackId,
        HlsMedia media,
        CancellationToken cancellationToken = default)
    {
        TranscodeInput? input;
        TranscodeVariantStatus status;
        if (media == HlsMedia.Video)
        {
            VideoSourceResult video = await streams.GetVideoSourceAsync(trackId, cancellationToken);
            status = StatusOf(video.Status);
            input = video.IsFound ? InputOf(video.Source) : null;
        }
        else
        {
            StreamSourceResult sound = await streams.GetSoundSourceAsync(trackId, cancellationToken);
            status = StatusOf(sound.Status);
            input = sound.IsFound ? InputOf(sound.Source) : null;
        }

        if (status != TranscodeVariantStatus.Found || input is null)
        {
            return TranscodeVariantResult.Of(status);
        }

        if (input.Duration <= TimeSpan.Zero)
        {
            return TranscodeVariantResult.Of(TranscodeVariantStatus.DurationUnknown);
        }

        TranscodeProfile profile = TranscodeProfiles.For(
            media,
            input,
            settings.Current,
            options.Value.SegmentDuration);
        return new TranscodeVariantResult(TranscodeVariantStatus.Found, TranscodeVariant.Of(input, profile));
    }

    public async Task<SegmentResult> GetSegmentAsync(
        Guid trackId,
        HlsMedia media,
        string variantId,
        int index,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        TranscodeVariantResult resolved = await ResolveAsync(trackId, media, cancellationToken);
        if (!resolved.IsFound)
        {
            return SegmentResult.Of(resolved.Status switch
            {
                TranscodeVariantStatus.TrackNotFound => SegmentStatus.TrackNotFound,
                TranscodeVariantStatus.NoMedia => SegmentStatus.NoMedia,
                TranscodeVariantStatus.FileMissing => SegmentStatus.FileMissing,
                _ => SegmentStatus.Failed,
            });
        }

        if (!string.Equals(resolved.Variant.Id, variantId, StringComparison.Ordinal))
        {
            return SegmentResult.Of(SegmentStatus.VariantOutdated);
        }

        if (!IsAvailable)
        {
            return SegmentResult.Of(SegmentStatus.Unavailable);
        }

        return await manager.GetSegmentAsync(resolved.Variant, index, userId, cancellationToken);
    }

    public TranscodingStatus GetStatus()
    {
        return new TranscodingStatus(
            hardware.Current,
            cache.IsAvailable,
            manager.ListJobs().Count,
            settings.Current.MaxParallelJobs);
    }

    public async Task<Page<TranscodeJobItem>> ListJobsAsync(
        int offset,
        int limit,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TranscodeJobInfo> jobs = manager.ListJobs();
        List<TranscodeJobInfo> page = [.. jobs.Skip(offset).Take(limit)];
        Guid[] trackIds = [.. page.Select(job => job.TrackId).Distinct()];
        IReadOnlyList<TrackRow> rows = trackIds.Length == 0
            ? []
            : await tracks.ListByIdsAsync(trackIds, userId, cancellationToken);
        var byId = rows.ToDictionary(row => row.Id);
        List<TranscodeJobItem> items = [.. page.Select(job => new TranscodeJobItem(job, byId.GetValueOrDefault(job.TrackId)))];
        return new Page<TranscodeJobItem>(items, offset, limit, jobs.Count);
    }

    public bool CancelJob(Guid id)
    {
        return manager.Cancel(id);
    }

    private static TranscodeVariantStatus StatusOf(StreamSourceStatus status)
    {
        return status switch
        {
            StreamSourceStatus.Found => TranscodeVariantStatus.Found,
            StreamSourceStatus.TrackNotFound => TranscodeVariantStatus.TrackNotFound,
            StreamSourceStatus.FileMissing => TranscodeVariantStatus.FileMissing,
            _ => TranscodeVariantStatus.NoMedia,
        };
    }

    private static TranscodeInput InputOf(VideoSource source)
    {
        return new TranscodeInput(
            source.TrackId,
            source.FileId,
            source.FullPath,
            source.Length,
            source.LastModified,
            source.Duration ?? TimeSpan.Zero,
            source.HasAudio,
            source.Width,
            source.Height);
    }

    private static TranscodeInput InputOf(StreamSource source)
    {
        return new TranscodeInput(
            source.TrackId,
            source.FileId,
            source.FullPath,
            source.Length,
            source.LastModified,
            source.Duration ?? TimeSpan.Zero,
            HasAudio: true);
    }
}
