namespace WaveMP.Service.Dtos;

/// <summary>
/// The ReplayGain tags of the audio file of a track. Missing tags are null; clients then leave the volume as it is.
/// </summary>
public sealed class ReplayGainDto
{
    /// <summary>
    /// Gain of the track in dB.
    /// </summary>
    public double? TrackGain { get; init; }

    /// <summary>
    /// Peak of the track, linear (1.0 is full scale).
    /// </summary>
    public double? TrackPeak { get; init; }

    /// <summary>
    /// Gain of the album in dB.
    /// </summary>
    public double? AlbumGain { get; init; }

    /// <summary>
    /// Peak of the album, linear (1.0 is full scale).
    /// </summary>
    public double? AlbumPeak { get; init; }
}
