namespace WaveMP.Core.Metadata.Files;

/// <summary>
/// Finds the files of the media folder by the relative paths the library stores.
/// </summary>
public interface IMediaFileLocator
{
    /// <summary>
    /// The absolute path of a file of the media folder from its path relative to the media folder (with <c>/</c> as
    /// separator). Returns <c>false</c> for rooted paths and for paths that leave the media folder (for example with
    /// <c>..</c>). Does not check whether the file exists.
    /// </summary>
    bool TryGetFullPath(string relativePath, out string fullPath);
}
