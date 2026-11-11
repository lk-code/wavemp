using WaveMP.Core.Streaming.Formats;

namespace WaveMP.Core.Streaming.Decisions;

/// <summary>
/// Decides between direct play and transcoding from the format of a file and the formats a client supports. Step 12
/// adds the capabilities for videos, bitrates and hardware acceleration here.
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
