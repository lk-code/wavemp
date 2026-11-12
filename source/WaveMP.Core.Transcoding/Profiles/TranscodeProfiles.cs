using WaveMP.Core.Settings.Groups;

namespace WaveMP.Core.Transcoding.Profiles;

/// <summary>
/// Chooses the profile of a transcode from the source, the settings and the segment length. Pure functions.
/// </summary>
public static class TranscodeProfiles
{
    /// <summary>
    /// Bitrate of the sound of videos in kbit/s.
    /// </summary>
    public const int VideoAudioBitrateKbps = 192;

    /// <summary>
    /// Bitrate of the picture in kbit/s for its height.
    /// </summary>
    public static int VideoBitrateKbpsFor(int height)
    {
        return height switch
        {
            <= 480 => 1500,
            <= 720 => 3500,
            <= 1080 => 6000,
            <= 1440 => 10000,
            _ => 16000,
        };
    }

    public static TranscodeProfile For(
        HlsMedia media,
        TranscodeInput input,
        TranscodingSettings settings,
        TimeSpan segmentDuration)
    {
        if (media == HlsMedia.Audio)
        {
            return new TranscodeProfile(
                HlsMedia.Audio,
                null,
                settings.MaxVideoHeight,
                0,
                settings.AudioBitrateKbps,
                segmentDuration);
        }

        var size = OutputSize.Of(input.Width, input.Height, settings.MaxVideoHeight);
        int height = size?.Height ?? settings.MaxVideoHeight;
        return new TranscodeProfile(
            HlsMedia.Video,
            size,
            settings.MaxVideoHeight,
            VideoBitrateKbpsFor(height),
            VideoAudioBitrateKbps,
            segmentDuration);
    }
}
