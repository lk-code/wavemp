namespace WaveMP.Core.Streaming.Formats;

/// <summary>
/// A codec of the picture of videos that clients may play directly.
/// </summary>
public enum VideoCodec
{
    /// <summary>
    /// H.264 (AVC).
    /// </summary>
    H264,

    /// <summary>
    /// H.265 (HEVC).
    /// </summary>
    Hevc,

    Vp8,

    Vp9,

    Av1,
}
