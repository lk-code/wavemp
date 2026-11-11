namespace WaveMP.Core.Caching.Stores;

/// <summary>
/// The folders of the cache and the names of its files. A name is the path of a file relative to the folder of its
/// area with <c>/</c> as separator; it never leaves that folder.
/// </summary>
internal static class CacheFolders
{
    public const string Covers = "covers";
    public const string Transcodes = "transcodes";

    /// <summary>
    /// Unfinished files; emptied on start.
    /// </summary>
    public const string Temporary = "tmp";

    public static IReadOnlyList<CacheArea> Areas { get; } = [CacheArea.Covers, CacheArea.Transcodes];

    public static string FolderOf(CacheArea area)
    {
        return area switch
        {
            CacheArea.Covers => Covers,
            CacheArea.Transcodes => Transcodes,
            _ => throw new ArgumentOutOfRangeException(nameof(area), area, "Unknown cache area."),
        };
    }

    /// <summary>
    /// Whether a name is valid: segments of letters, digits, <c>.</c>, <c>-</c> and <c>_</c>, without <c>.</c> or
    /// <c>..</c> as segment.
    /// </summary>
    public static bool IsValidName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        foreach (string segment in name.Split('/'))
        {
            if (segment.Length == 0 || segment is "." or ".."
                || !segment.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_'))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// The full path of a file of an area; <c>false</c> for invalid names.
    /// </summary>
    public static bool TryGetPath(string areaPath, string name, out string fullPath)
    {
        fullPath = string.Empty;
        if (!IsValidName(name))
        {
            return false;
        }

        fullPath = Path.Combine([areaPath, .. name.Split('/')]);
        return true;
    }

    /// <summary>
    /// The name of a file inside the folder of an area.
    /// </summary>
    public static string NameOf(string areaPath, string fullPath)
    {
        return Path.GetRelativePath(areaPath, fullPath).Replace(Path.DirectorySeparatorChar, '/');
    }
}
