namespace WaveMP.Core.Metadata.Paths;

/// <summary>
/// Values read from a file name (without extension).
/// </summary>
public sealed record FileNameFields(
    int? TrackNumber = null,
    int? DiscNumber = null,
    string? Artist = null,
    string? Album = null,
    string? Title = null);
