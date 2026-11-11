namespace WaveMP.Core.Caching.Covers;

/// <summary>
/// The sizes of scaled covers: the largest width and height in pixels. Clients may ask for any size from
/// <see cref="MinRequested"/> to <see cref="MaxRequested"/>; it is rounded up to the next defined size, at most
/// <see cref="Large"/>, so the cache stays small.
/// </summary>
public static class CoverSizes
{
    /// <summary>
    /// Lists and small covers, e.g. the mini player.
    /// </summary>
    public const int Small = 96;

    /// <summary>
    /// Tiles and detail pages.
    /// </summary>
    public const int Medium = 300;

    /// <summary>
    /// Large covers on screens with a high pixel density; created only when they are asked for.
    /// </summary>
    public const int Large = 1000;

    public const int MinRequested = 1;
    public const int MaxRequested = 4096;

    public static IReadOnlyList<int> All { get; } = [Small, Medium, Large];

    /// <summary>
    /// The sizes prepared after a scan.
    /// </summary>
    public static IReadOnlyList<int> Prepared { get; } = [Small, Medium];

    /// <summary>
    /// The smallest defined size that is at least as large as the requested one, otherwise the largest.
    /// </summary>
    public static int Snap(int requested)
    {
        foreach (int size in All)
        {
            if (size >= requested)
            {
                return size;
            }
        }

        return Large;
    }
}
