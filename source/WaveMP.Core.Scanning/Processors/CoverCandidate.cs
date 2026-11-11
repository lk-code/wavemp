using WaveMP.Data.Entities;

namespace WaveMP.Core.Scanning.Processors;

/// <summary>
/// A cover available for a track: a picture file of its group or a picture embedded in one of its files.
/// </summary>
public sealed record CoverCandidate(MediaFile File, CoverSource Source, int? Width, int? Height, string? MimeType);
