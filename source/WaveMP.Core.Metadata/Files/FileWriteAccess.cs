namespace WaveMP.Core.Metadata.Files;

/// <summary>
/// Opens the file for writing and closes it at once, without writing; neither content nor change time change.
/// </summary>
internal sealed class FileWriteAccess : IFileWriteAccess
{
    public bool IsWritable(string path)
    {
        try
        {
            if (new FileInfo(path).IsReadOnly)
            {
                return false;
            }

            using FileStream stream = new(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
