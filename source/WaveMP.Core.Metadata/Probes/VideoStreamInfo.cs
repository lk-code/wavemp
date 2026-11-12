namespace WaveMP.Core.Metadata.Probes;

/// <summary>
/// A video stream, or the picture of a picture file. <paramref name="PixelFormat"/> is the pixel format ffprobe
/// reports, e.g. <c>yuv420p</c> or <c>yuv420p10le</c>.
/// </summary>
public sealed record VideoStreamInfo(
    string? Codec,
    int? Width,
    int? Height,
    long? Bitrate,
    string? PixelFormat = null);
