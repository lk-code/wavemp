namespace WaveMP.Core.Metadata.Files;

/// <summary>
/// Checks whether WaveMP may write a file, without changing it.
/// </summary>
public interface IFileWriteAccess
{
    /// <summary>
    /// <c>false</c> for read-only files, files without write permission and files on read-only mounts.
    /// </summary>
    bool IsWritable(string path);
}
