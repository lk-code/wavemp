using WaveMP.Core.Transcoding.Profiles;
using WaveMP.Core.Transcoding.Transcodes;
using WaveMP.Data.Queries;

namespace WaveMP.Core.Transcoding.Services;

/// <summary>
/// The HLS streams of tracks: the variant a track is transcoded to, its segments and the state of the transcoding.
/// </summary>
public interface ITranscodingService
{
    /// <summary>
    /// Whether anything can be transcoded (ffmpeg can be started and the cache can be written).
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// The variant of the track for the media, from its file as found on the disk and the current settings.
    /// </summary>
    Task<TranscodeVariantResult> ResolveAsync(
        Guid trackId,
        HlsMedia media,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// A segment of the variant <paramref name="variantId"/> of the track; transcoded first if needed.
    /// <see cref="SegmentStatus.VariantOutdated"/> if the variant of the track changed since the playlist was read.
    /// </summary>
    Task<SegmentResult> GetSegmentAsync(
        Guid trackId,
        HlsMedia media,
        string variantId,
        int index,
        Guid? userId,
        CancellationToken cancellationToken = default);

    TranscodingStatus GetStatus();

    /// <summary>
    /// The running transcodes, newest first, with their tracks as seen by <paramref name="userId"/>.
    /// </summary>
    Task<Page<TranscodeJobItem>> ListJobsAsync(
        int offset,
        int limit,
        Guid? userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops a running transcode; <c>false</c> if there is none with the id.
    /// </summary>
    bool CancelJob(Guid id);
}
