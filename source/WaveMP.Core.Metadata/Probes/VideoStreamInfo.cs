namespace WaveMP.Core.Metadata.Probes;

/// <summary>
/// A video stream, or the picture of a picture file.
/// </summary>
public sealed record VideoStreamInfo(string? Codec, int? Width, int? Height, long? Bitrate);
