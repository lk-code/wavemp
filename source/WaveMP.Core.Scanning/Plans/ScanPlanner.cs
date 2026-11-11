using WaveMP.Core.Scanning.Discoveries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Scanning.Plans;

/// <summary>
/// Compares the folders on disk with the stored files: a file is new, changed (other size, other change time, other
/// category, read by an older version of the reader or known to be changed), unchanged or gone. Folders without
/// changes are skipped.
/// </summary>
public static class ScanPlanner
{
    /// <param name="discovered">The folders found on disk.</param>
    /// <param name="stored">The stored files of these folders (and of folders that may be gone).</param>
    /// <param name="metadataVersion">The current version of the metadata reader.</param>
    /// <param name="changedFiles">Relative paths of stored files that are read again even with the same size and
    /// change time, e.g. after WaveMP wrote their tags in place (file systems with a coarse change time, padding of
    /// the tags).</param>
    public static ScanPlan Plan(
        IReadOnlyList<DiscoveredDirectory> discovered,
        IReadOnlyList<MediaFileSnapshot> stored,
        int metadataVersion,
        IReadOnlySet<string>? changedFiles = null)
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
                    || snapshot.MetadataVersion < metadataVersion
                    || changedFiles?.Contains(file.RelativePath) == true)
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
