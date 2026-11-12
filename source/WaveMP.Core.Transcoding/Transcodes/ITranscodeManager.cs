using WaveMP.Core.Transcoding.Profiles;

namespace WaveMP.Core.Transcoding.Transcodes;

/// <summary>
/// Runs the ffmpeg processes of the transcodes: starts one when a segment is missing, lets requests wait for their
/// segment, starts a new one at the segment of a seek, keeps to the limit of parallel transcodes (giving the slot of
/// an idle transcode to a new one), stops idle transcodes and removes their temporary folders. Finished segments go
/// into the cache, so a segment is transcoded once per file version and profile.
/// </summary>
public interface ITranscodeManager
{
    /// <summary>
    /// The segment of the variant from the cache, transcoding it first if needed.
    /// </summary>
    Task<SegmentResult> GetSegmentAsync(
        TranscodeVariant variant,
        int index,
        Guid? userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The running transcodes, newest first.
    /// </summary>
    IReadOnlyList<TranscodeJobInfo> ListJobs();

    /// <summary>
    /// Stops a running transcode; <c>false</c> if there is none with the id.
    /// </summary>
    bool Cancel(Guid id);

    /// <summary>
    /// Stops the transcodes no client asked for longer than the idle time. Returns how many were stopped.
    /// </summary>
    int StopIdle();
}
