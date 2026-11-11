using WaveMP.Data.Entities;

namespace WaveMP.Core.Streaming.Sources;

/// <summary>
/// The ReplayGain tags of an audio file. Gains are in dB, peaks are linear (1.0 is full scale). Missing tags are
/// <c>null</c>; clients then leave the volume as it is.
/// </summary>
public sealed record ReplayGainValues(double? TrackGain, double? TrackPeak, double? AlbumGain, double? AlbumPeak)
{
    public static readonly ReplayGainValues None = new(null, null, null, null);

    public static ReplayGainValues From(MediaFileTags? tags)
    {
        return tags is null
            ? None
            : new ReplayGainValues(
                tags.ReplayGainTrackGain,
                tags.ReplayGainTrackPeak,
                tags.ReplayGainAlbumGain,
                tags.ReplayGainAlbumPeak);
    }
}
