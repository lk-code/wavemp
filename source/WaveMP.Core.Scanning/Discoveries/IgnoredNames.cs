namespace WaveMP.Core.Scanning.Discoveries;

/// <summary>
/// Files and folders the scan skips: hidden names (starting with a dot) and the folders that NAS systems and
/// operating systems create for thumbnails, recycle bins and snapshots.
/// </summary>
public static class IgnoredNames
{
    private static readonly HashSet<string> _names = new(StringComparer.OrdinalIgnoreCase)
    {
        "@eaDir",
        "#recycle",
        "#snapshot",
        "$RECYCLE.BIN",
        "System Volume Information",
        "lost+found",
    };

    public static bool IsIgnored(string name)
    {
        return name.Length == 0 || name.StartsWith('.') || _names.Contains(name);
    }

    /// <summary>
    /// Whether any folder of a relative path (with <c>/</c>) is ignored.
    /// </summary>
    public static bool IsIgnoredPath(string relativePath)
    {
        return relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries).Any(IsIgnored);
    }
}
