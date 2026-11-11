namespace WaveMP.Core.Caching.Stores;

/// <summary>
/// Chooses the files the clean-up removes (least recently used first): nothing while the cache is within its limit,
/// otherwise the files used longest ago until it is at most the target share of the limit.
/// </summary>
internal static class LruTrimPlanner
{
    public static IReadOnlyList<CacheIndexEntry> Plan(
        IEnumerable<CacheIndexEntry> entries,
        long totalBytes,
        long maxBytes,
        double targetRatio)
    {
        if (totalBytes <= maxBytes)
        {
            return [];
        }

        long target = (long)(maxBytes * Math.Clamp(targetRatio, 0, 1));
        List<CacheIndexEntry> planned = [];
        long remaining = totalBytes;
        foreach (CacheIndexEntry entry in entries
            .OrderBy(entry => entry.LastUsedAt)
            .ThenBy(entry => entry.Area)
            .ThenBy(entry => entry.Name, StringComparer.Ordinal))
        {
            if (remaining <= target)
            {
                break;
            }

            planned.Add(entry);
            remaining -= entry.Size;
        }

        return planned;
    }
}
