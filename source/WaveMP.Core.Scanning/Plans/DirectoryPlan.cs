using WaveMP.Core.Scanning.Discoveries;

namespace WaveMP.Core.Scanning.Plans;

/// <summary>
/// A folder whose files changed since the last scan.
/// </summary>
/// <param name="RelativePath">Relative path of the folder.</param>
/// <param name="Files">All media files of the folder on disk.</param>
/// <param name="FilesToRead">Files that are new or changed and must be read.</param>
/// <param name="Added">Number of new files.</param>
/// <param name="Updated">Number of changed files.</param>
/// <param name="Removed">Number of files that are gone.</param>
public sealed record DirectoryPlan(
    string RelativePath,
    IReadOnlyList<DiscoveredFile> Files,
    IReadOnlyList<DiscoveredFile> FilesToRead,
    int Added,
    int Updated,
    int Removed);
