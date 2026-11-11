namespace WaveMP.Core.Scanning.Plans;

/// <summary>
/// What a scan has to do: the changed folders and the folders that are gone.
/// </summary>
/// <param name="ChangedDirectories">Folders with new, changed or removed files.</param>
/// <param name="DeletedDirectories">Stored folders that no longer exist on disk.</param>
/// <param name="DirectoriesFound">Folders found on disk.</param>
/// <param name="FilesFound">Media files found on disk.</param>
/// <param name="FilesUnchanged">Files that need not be read again.</param>
/// <param name="FilesInDeletedDirectories">Stored files of the folders that are gone.</param>
public sealed record ScanPlan(
    IReadOnlyList<DirectoryPlan> ChangedDirectories,
    IReadOnlyList<string> DeletedDirectories,
    int DirectoriesFound,
    int FilesFound,
    int FilesUnchanged,
    int FilesInDeletedDirectories);
