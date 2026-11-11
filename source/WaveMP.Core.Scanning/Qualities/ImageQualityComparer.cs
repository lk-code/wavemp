using WaveMP.Data.Entities;

namespace WaveMP.Core.Scanning.Qualities;

/// <summary>
/// Sorts picture files best first, see <see cref="ImageQuality"/>.
/// </summary>
public sealed class ImageQualityComparer : IComparer<MediaFile>
{
    public static ImageQualityComparer Instance { get; } = new();

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

        return ImageQuality.Compare(
            (x.Issue is null, x.Width, x.Height, x.Size, x.RelativePath),
            (y.Issue is null, y.Width, y.Height, y.Size, y.RelativePath));
    }
}
