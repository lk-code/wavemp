namespace WaveMP.Core.Caching.Stores;

/// <summary>
/// The space an area of the cache uses.
/// </summary>
/// <param name="SizeBytes">Size of its files in bytes.</param>
/// <param name="EntryCount">Number of its files.</param>
public sealed record CacheAreaUsage(long SizeBytes, int EntryCount)
{
    public static CacheAreaUsage Empty { get; } = new(0, 0);
}
