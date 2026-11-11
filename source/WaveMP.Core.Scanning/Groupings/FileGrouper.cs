using WaveMP.Data.Entities;

namespace WaveMP.Core.Scanning.Groupings;

/// <summary>
/// Groups the files of a folder by their base name, without regard to case.
/// </summary>
public static class FileGrouper
{
    public static IReadOnlyList<FileGroup> Group(IEnumerable<MediaFile> files)
    {
        return [.. files
            .GroupBy(file => file.NormalizedBaseName, StringComparer.Ordinal)
            .Select(group =>
            {
                MediaFile[] members = [.. group.OrderBy(file => file.RelativePath, StringComparer.Ordinal)];
                return new FileGroup(group.Key, members[0].BaseName, members);
            })
            .OrderBy(group => group.NormalizedBaseName, StringComparer.Ordinal)];
    }

    /// <summary>
    /// The key files are grouped by.
    /// </summary>
    public static string NormalizeBaseName(string baseName)
    {
        return baseName.ToUpperInvariant();
    }
}
