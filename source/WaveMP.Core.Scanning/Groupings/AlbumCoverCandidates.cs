using WaveMP.Core.Metadata.Paths;
using WaveMP.Core.Scanning.Qualities;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Scanning.Groupings;

/// <summary>
/// Finds the covers of albums: pictures named after the cover names of the settings (e.g. <c>cover.jpg</c>) in the
/// folders of the album's tracks, and for disc folders such as <c>CD1</c> also in the folder above. The best one
/// comes first: by the order of the cover names, then by quality.
/// </summary>
public static class AlbumCoverCandidates
{
    public static IReadOnlyDictionary<Guid, IReadOnlyList<AlbumCoverImage>> Select(
        IReadOnlyList<AlbumFolder> albumFolders,
        IReadOnlyList<AlbumCoverImage> images,
        IReadOnlyList<string> coverNames)
    {
        var priority = coverNames
            .Select((name, index) => (Key: FileGrouper.NormalizeBaseName(name), Index: index))
            .DistinctBy(entry => entry.Key)
            .ToDictionary(entry => entry.Key, entry => entry.Index, StringComparer.Ordinal);
        ILookup<string, AlbumCoverImage> imagesByDirectory = images
            .Where(image => priority.ContainsKey(image.NormalizedBaseName))
            .ToLookup(image => image.DirectoryPath, StringComparer.Ordinal);

        Dictionary<Guid, IReadOnlyList<AlbumCoverImage>> covers = [];
        foreach (IGrouping<Guid, AlbumFolder> album in albumFolders.GroupBy(folder => folder.AlbumId))
        {
            HashSet<string> directories = new(StringComparer.Ordinal);
            foreach (AlbumFolder folder in album)
            {
                directories.Add(folder.DirectoryPath);
                if (ParentOfDiscFolder(folder.DirectoryPath) is { } parent)
                {
                    directories.Add(parent);
                }
            }

            List<AlbumCoverImage> candidates = [.. directories.SelectMany(directory => imagesByDirectory[directory])];
            candidates.Sort((x, y) =>
            {
                int result = priority[x.NormalizedBaseName].CompareTo(priority[y.NormalizedBaseName]);
                return result != 0
                    ? result
                    : ImageQuality.Compare(
                        (true, x.Width, x.Height, x.Size, x.RelativePath),
                        (true, y.Width, y.Height, y.Size, y.RelativePath));
            });

            if (candidates.Count > 0)
            {
                covers[album.Key] = candidates;
            }
        }

        return covers;
    }

    /// <summary>
    /// The folder above a disc folder such as <c>Album/CD1</c>, otherwise <c>null</c>.
    /// </summary>
    public static string? ParentOfDiscFolder(string directoryPath)
    {
        int slash = directoryPath.LastIndexOf('/');
        string name = slash < 0 ? directoryPath : directoryPath[(slash + 1)..];
        return DiscFolders.TryParse(name, out _) ? (slash < 0 ? string.Empty : directoryPath[..slash]) : null;
    }
}
