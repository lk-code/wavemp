using WaveMP.Core.Scanning.Discoveries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Scanning.Plans;

/// <summary>
/// Compares the folders on disk with the stored files: a file is new, changed (other size, other change time, other
/// category or read by an older version of the reader), unchanged or gone. Folders without changes are skipped.
/// </summary>
public static class ScanPlanner
{
    public static ScanPlan Plan(
        IReadOnlyList<DiscoveredDirectory> discovered,
        IReadOnlyList<MediaFileSnapshot> stored,
        int metadataVersion)
    {
        var storedByDirectory = stored
            .GroupBy(file => file.DirectoryPath, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        List<DirectoryPlan> changed = [];
        int unchanged = 0;
        foreach (DiscoveredDirectory directory in discovered)
        {
            Dictionary<string, MediaFileSnapshot> storedFiles = storedByDirectory
                .GetValueOrDefault(directory.RelativePath, [])
                .ToDictionary(file => file.RelativePath, StringComparer.Ordinal);

            List<DiscoveredFile> toRead = [];
            int added = 0;
            int updated = 0;
            foreach (DiscoveredFile file in directory.Files)
            {
                if (!storedFiles.Remove(file.RelativePath, out MediaFileSnapshot? snapshot))
                {
                    added++;
                    toRead.Add(file);
                }
                else if (snapshot.Size != file.Size
                    || snapshot.ModifiedAt != file.ModifiedAt
                    || snapshot.Kind != file.Kind
                    || snapshot.MetadataVersion < metadataVersion)
                {
                    updated++;
                    toRead.Add(file);
                }
                else
                {
                    unchanged++;
                }
            }

            int removed = storedFiles.Count;
            if (added + updated + removed > 0)
            {
                changed.Add(new DirectoryPlan(directory.RelativePath, directory.Files, toRead, added, updated, removed));
            }
        }

        HashSet<string> found = [.. discovered.Select(directory => directory.RelativePath)];
        string[] deleted = [.. storedByDirectory.Keys.Where(directory => !found.Contains(directory)).Order()];

        return new ScanPlan(
            changed,
            deleted,
            discovered.Count,
            discovered.Sum(directory => directory.Files.Count),
            unchanged,
            deleted.Sum(directory => storedByDirectory[directory].Length));
    }
}
