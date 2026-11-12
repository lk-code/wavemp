using WaveMP.Core.Streaming.Formats;

namespace WaveMP.Core.Streaming.Decisions;

/// <summary>
/// Decides between direct play and transcoding from the format of an audio file and the formats a client supports.
/// Videos are decided by <see cref="VideoPlaybackDecider"/>.
/// </summary>
public static class PlaybackDecider
{
    public static PlaybackDecision Decide(AudioFormat? format, IReadOnlySet<AudioFormat> supported)
    {
        ArgumentNullException.ThrowIfNull(supported);

        if (format is not { } known)
        {
            return new PlaybackDecision(PlaybackMethod.Transcode, null, TranscodeReason.FormatUnknown);
        }

        return supported.Contains(known)
            ? new PlaybackDecision(PlaybackMethod.DirectPlay, known, null)
            : new PlaybackDecision(PlaybackMethod.Transcode, known, TranscodeReason.FormatUnsupported);
    }
}
