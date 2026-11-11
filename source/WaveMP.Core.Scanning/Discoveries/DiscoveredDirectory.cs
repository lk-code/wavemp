namespace WaveMP.Core.Scanning.Discoveries;

/// <summary>
/// A folder found on disk with its media files (not those of its subfolders).
/// </summary>
/// <param name="RelativePath">Path relative to the media folder with <c>/</c>; empty for the media folder.</param>
/// <param name="Files">The media files of the folder.</param>
public sealed record DiscoveredDirectory(string RelativePath, IReadOnlyList<DiscoveredFile> Files);
