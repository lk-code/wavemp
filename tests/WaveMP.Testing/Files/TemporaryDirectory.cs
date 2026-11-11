namespace WaveMP.Testing.Files;

/// <summary>
/// A new, empty folder below the system's temporary folder that is deleted with everything in it on dispose.
/// </summary>
public sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wavemp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    /// <summary>
    /// Absolute path of the folder.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Absolute path of a file or folder inside, from segments separated by <c>/</c>.
    /// </summary>
    public string Combine(string relativePath)
    {
        return System.IO.Path.Combine([Path, .. relativePath.Split('/')]);
    }

    /// <summary>
    /// Creates the folders of <paramref name="relativePath"/> and returns its absolute path.
    /// </summary>
    public string CreateDirectory(string relativePath)
    {
        return Directory.CreateDirectory(Combine(relativePath)).FullName;
    }

    public void Dispose()
    {
        if (!Directory.Exists(Path))
        {
            return;
        }

        // Read-only files cannot be deleted on Windows.
        foreach (string file in Directory.EnumerateFiles(Path, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // A process may still hold a file for a moment; the folder lies in the temporary folder anyway.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
