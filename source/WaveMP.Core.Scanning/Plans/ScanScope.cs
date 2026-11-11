namespace WaveMP.Core.Scanning.Plans;

/// <summary>
/// What a scan covers: the whole media folder, or some folders with all their subfolders (e.g. after changes reported
/// by the file watcher).
/// </summary>
public sealed class ScanScope
{
    private ScanScope(IReadOnlyList<string> directories)
    {
        Directories = directories;
    }

    public static ScanScope Full { get; } = new([string.Empty]);

    /// <summary>
    /// Relative paths of the folders, without nested ones; a single empty path for the whole media folder.
    /// </summary>
    public IReadOnlyList<string> Directories { get; }

    public bool IsFull => Directories.Count == 1 && Directories[0].Length == 0;

    /// <summary>
    /// A scope of folders given as relative paths (with <c>/</c> or <c>\</c>). Folders inside other folders of the
    /// list are left out; the media folder itself makes it a full scan.
    /// </summary>
    public static ScanScope Of(IEnumerable<string> directories)
    {
        string[] normalized = [.. directories
            .Select(Normalize)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];
        if (normalized.Length == 0 || normalized.Contains(string.Empty))
        {
            return Full;
        }

        List<string> reduced = [];
        foreach (string directory in normalized)
        {
            if (!reduced.Any(parent => directory.StartsWith(parent + "/", StringComparison.Ordinal)))
            {
                reduced.Add(directory);
            }
        }

        return new ScanScope(reduced);
    }

    /// <summary>
    /// Whether a folder lies in the scope.
    /// </summary>
    public bool Contains(string directory)
    {
        return IsFull || Directories.Any(scope => directory == scope
            || directory.StartsWith(scope + "/", StringComparison.Ordinal));
    }

    public static string Normalize(string directory)
    {
        return string.Join('/', directory
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(segment => segment != "."));
    }
}
