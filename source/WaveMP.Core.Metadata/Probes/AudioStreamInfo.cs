namespace WaveMP.Core.Metadata.Probes;

/// <summary>
/// An audio stream. <paramref name="BitDepth"/> is only known for lossless codecs.
/// </summary>
public sealed record AudioStreamInfo(string? Codec, long? Bitrate, int? SampleRate, int? Channels, int? BitDepth);
