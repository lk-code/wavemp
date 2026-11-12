using WaveMP.Core.Streaming.Formats;
using WaveMP.Core.Streaming.Sources;

namespace WaveMP.Core.Streaming.Decisions;

/// <summary>
/// Decides between direct play and transcoding of a video from its container and codecs and what the client plays.
/// The checks run from the outside in: container, picture, sound.
/// </summary>
public static class VideoPlaybackDecider
{
    public static VideoPlaybackDecision Decide(VideoSource source, VideoCapabilities capabilities)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(capabilities);

        // Read by an older scan, the sound and the pixel format are unknown: HLS is safe.
        if (!source.MetadataComplete || source.Container is not { } container)
        {
            return VideoPlaybackDecision.Transcode(TranscodeReason.FormatUnknown);
        }

        if (!capabilities.Containers.Contains(container))
        {
            return VideoPlaybackDecision.Transcode(TranscodeReason.ContainerUnsupported);
        }

        if (source.VideoCodec is not { } videoCodec
            || !capabilities.VideoCodecs.Contains(videoCodec)
            || (videoCodec == VideoCodec.H264 && !VideoFormats.IsEightBit420(source.PixelFormat)))
        {
            return VideoPlaybackDecision.Transcode(TranscodeReason.VideoCodecUnsupported);
        }

        if (source.HasAudio
            && (source.AudioCodec is not { } audioCodec || !capabilities.AudioCodecs.Contains(audioCodec)))
        {
            return VideoPlaybackDecision.Transcode(TranscodeReason.AudioCodecUnsupported);
        }

        return VideoPlaybackDecision.DirectPlay;
    }
}
