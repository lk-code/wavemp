using WaveMP.Data.Entities;

namespace WaveMP.Core.Scanning.Qualities;

/// <summary>
/// Sorts audio files of a track best first: readable before damaged, lossless before lossy; lossless by bit depth,
/// sample rate and bitrate, lossy by bitrate and sample rate; then the larger file and finally the path, so the order
/// is always the same.
/// </summary>
public sealed class AudioQualityComparer : IComparer<MediaFile>
{
    public static AudioQualityComparer Instance { get; } = new();

    public int Compare(MediaFile? x, MediaFile? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null || y is null)
        {
            return x is null ? 1 : -1;
        }

        int result = (x.Issue is null).CompareTo(y.Issue is null);
        if (result == 0)
        {
            result = x.IsLossless.CompareTo(y.IsLossless);
        }

        if (result == 0 && x.IsLossless)
        {
            result = Compare(x.BitDepth, y.BitDepth);
            result = result != 0 ? result : Compare(x.SampleRate, y.SampleRate);
            result = result != 0 ? result : Compare(x.Bitrate, y.Bitrate);
        }
        else if (result == 0)
        {
            result = Compare(x.Bitrate, y.Bitrate);
            result = result != 0 ? result : Compare(x.SampleRate, y.SampleRate);
        }

        result = result != 0 ? result : x.Size.CompareTo(y.Size);

        // Higher values first; the path ascending.
        return result != 0 ? -result : string.CompareOrdinal(x.RelativePath, y.RelativePath);
    }

    internal static int Compare(int? x, int? y)
    {
        return (x ?? 0).CompareTo(y ?? 0);
    }
}
