namespace WaveMP.Testing.Media;

/// <summary>
/// Codecs of the synthetic test videos, each with the sound codec that usually goes with it.
/// </summary>
public enum TestVideoCodec
{
    /// <summary>
    /// MPEG-4 part 2 with AAC; browsers cannot play it directly.
    /// </summary>
    Mpeg4,

    /// <summary>
    /// H.264 (8 bit, 4:2:0) with AAC.
    /// </summary>
    H264,

    /// <summary>
    /// VP9 with Opus, for WebM.
    /// </summary>
    Vp9,
}
