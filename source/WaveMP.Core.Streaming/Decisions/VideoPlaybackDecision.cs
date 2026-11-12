namespace WaveMP.Core.Streaming.Decisions;

/// <summary>
/// Whether a client plays a video directly or needs it transcoded.
/// </summary>
/// <param name="Method">Direct play or transcoding.</param>
/// <param name="Reason">Why the video has to be transcoded; <c>null</c> for direct play.</param>
public sealed record VideoPlaybackDecision(PlaybackMethod Method, TranscodeReason? Reason)
{
    public static VideoPlaybackDecision DirectPlay { get; } = new(PlaybackMethod.DirectPlay, null);

    public static VideoPlaybackDecision Transcode(TranscodeReason reason)
    {
        return new VideoPlaybackDecision(PlaybackMethod.Transcode, reason);
    }
}
