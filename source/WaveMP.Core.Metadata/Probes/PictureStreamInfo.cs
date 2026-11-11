namespace WaveMP.Core.Metadata.Probes;

/// <summary>
/// A picture embedded in a media file.
/// </summary>
public sealed record PictureStreamInfo(string? Codec, int? Width, int? Height);
