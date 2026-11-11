namespace WaveMP.Core.Scanning.Qualities;

/// <summary>
/// Compares pictures best first: readable before damaged, then by resolution (pixels), size and path.
/// </summary>
public static class ImageQuality
{
    public static int Compare(
        (bool Readable, int? Width, int? Height, long Size, string RelativePath) x,
        (bool Readable, int? Width, int? Height, long Size, string RelativePath) y)
    {
        int result = x.Readable.CompareTo(y.Readable);
        result = result != 0 ? result : Pixels(x.Width, x.Height).CompareTo(Pixels(y.Width, y.Height));
        result = result != 0 ? result : x.Size.CompareTo(y.Size);
        return result != 0 ? -result : string.CompareOrdinal(x.RelativePath, y.RelativePath);
    }

    private static long Pixels(int? width, int? height)
    {
        return (long)(width ?? 0) * (height ?? 0);
    }
}
