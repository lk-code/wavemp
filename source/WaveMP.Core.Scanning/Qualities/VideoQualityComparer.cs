using WaveMP.Data.Entities;

namespace WaveMP.Core.Scanning.Qualities;

/// <summary>
/// Sorts video files of a track best first: readable before damaged, then by resolution (pixels), bitrate, size and
/// path.
/// </summary>
public sealed class VideoQualityComparer : IComparer<MediaFile>
{
    public static VideoQualityComparer Instance { get; } = new();

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
        result = result != 0 ? result : Pixels(x).CompareTo(Pixels(y));
        result = result != 0 ? result : AudioQualityComparer.Compare(x.Bitrate, y.Bitrate);
        result = result != 0 ? result : x.Size.CompareTo(y.Size);
        return result != 0 ? -result : string.CompareOrdinal(x.RelativePath, y.RelativePath);
    }

    internal static long Pixels(MediaFile file)
    {
        return (long)(file.Width ?? 0) * (file.Height ?? 0);
    }
}
