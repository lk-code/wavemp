namespace WaveMP.Core.Transcoding.Profiles;

/// <summary>
/// What a transcode produces: H.264 video (for <see cref="HlsMedia.Video"/>) and AAC stereo sound in MPEG-TS
/// segments of the same length.
/// </summary>
/// <param name="Media">Video with sound or sound only.</param>
/// <param name="Size">Size of the picture, if the size of the source is known; otherwise the source is limited to
/// <paramref name="MaxHeight"/>.</param>
/// <param name="MaxHeight">Largest height of the picture.</param>
/// <param name="VideoBitrateKbps">Bitrate of the picture in kbit/s; 0 for sound only.</param>
/// <param name="AudioBitrateKbps">Bitrate of the sound in kbit/s.</param>
/// <param name="SegmentDuration">Length of a segment.</param>
public sealed record TranscodeProfile(
    HlsMedia Media,
    OutputSize? Size,
    int MaxHeight,
    int VideoBitrateKbps,
    int AudioBitrateKbps,
    TimeSpan SegmentDuration)
{
    /// <summary>
    /// Sample rate of the transcoded sound.
    /// </summary>
    public const int SampleRate = 48000;

    /// <summary>
    /// Peak bandwidth for the master playlist in bit/s, with headroom for peaks of the picture and the MPEG-TS
    /// overhead.
    /// </summary>
    public int Bandwidth => ((VideoBitrateKbps * 3 / 2) + AudioBitrateKbps) * 1100;
}
