using System.Text;
using WaveMP.Data.Entities;

namespace WaveMP.Core.Scanning.Discoveries;

/// <summary>
/// Walks a folder of the media folder and all its subfolders and lists their media files, folder by folder. Ignored
/// names are skipped, inaccessible entries too. Links to folders are not followed (no cycles); links to files are.
/// </summary>
public static class MediaFolderWalker
{
    private static readonly EnumerationOptions _options = new()
    {
        IgnoreInaccessible = true,
        RecurseSubdirectories = false,
        // Covers such as folder.jpg are often hidden on Windows; hidden names starting with a dot are ignored below.
        AttributesToSkip = FileAttributes.None,
    };

    /// <param name="rootPath">Absolute path of the media folder.</param>
    /// <param name="startDirectory">Relative path of the folder to walk; empty for the whole media folder.</param>
    /// <param name="classifier">Decides which files are media files.</param>
    /// <param name="maxPathBytes">Longest relative path in UTF-8 bytes.</param>
    /// <param name="pathTooLong">Called with the relative path of every file that is skipped for its length.</param>
    /// <param name="cancellationToken">Stops the walk.</param>
    public static IEnumerable<DiscoveredDirectory> Walk(
        string rootPath,
        string startDirectory,
        MediaKindClassifier classifier,
        int maxPathBytes,
        Action<string> pathTooLong,
        CancellationToken cancellationToken)
    {
        if (IgnoredNames.IsIgnoredPath(startDirectory))
        {
            yield break;
        }

        DirectoryInfo start = new(Path.Combine(rootPath, ToSystemPath(startDirectory)));
        if (!start.Exists || start.LinkTarget is not null && startDirectory.Length > 0)
        {
            yield break;
        }

        Stack<(DirectoryInfo Directory, string RelativePath)> pending = new();
        pending.Push((start, startDirectory));
        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            (DirectoryInfo directory, string relativePath) = pending.Pop();

            List<DiscoveredFile> files = [];
            List<(DirectoryInfo, string)> subdirectories = [];
            foreach (FileSystemInfo entry in Enumerate(directory))
            {
                if (IgnoredNames.IsIgnored(entry.Name))
                {
                    continue;
                }

                string entryPath = relativePath.Length == 0 ? entry.Name : relativePath + "/" + entry.Name;
                if (entry is DirectoryInfo subdirectory)
                {
                    if (subdirectory.LinkTarget is null)
                    {
                        subdirectories.Add((subdirectory, entryPath));
                    }

                    continue;
                }

                if (entry is not FileInfo file || classifier.Classify(file.Name) is not { } kind)
                {
                    continue;
                }

                if (Encoding.UTF8.GetByteCount(entryPath) > maxPathBytes)
                {
                    pathTooLong(entryPath);
                    continue;
                }

                if (Describe(file, entryPath, kind) is { } discovered)
                {
                    files.Add(discovered);
                }
            }

            // Sorted, so folders are processed in a stable order.
            foreach ((DirectoryInfo, string) subdirectory in subdirectories.OrderByDescending(
                item => item.Item2,
                StringComparer.Ordinal))
            {
                pending.Push(subdirectory);
            }

            yield return new DiscoveredDirectory(
                relativePath,
                [.. files.OrderBy(file => file.RelativePath, StringComparer.Ordinal)]);
        }
    }

    /// <summary>
    /// The time of the last change in the precision of PostgreSQL (microseconds), so a stored time compares equal.
    /// </summary>
    public static DateTimeOffset TruncateToMicroseconds(DateTimeOffset value)
    {
        return new DateTimeOffset(value.UtcTicks - (value.UtcTicks % 10), TimeSpan.Zero);
    }

    public static string ToSystemPath(string relativePath)
    {
        return relativePath.Replace('/', Path.DirectorySeparatorChar);
    }

    private static IEnumerable<FileSystemInfo> Enumerate(DirectoryInfo directory)
    {
        try
        {
            return [.. directory.EnumerateFileSystemInfos("*", _options)];
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static DiscoveredFile? Describe(FileInfo file, string relativePath, MediaFileKind kind)
    {
        try
        {
            FileInfo target = file.LinkTarget is null
                ? file
                : file.ResolveLinkTarget(returnFinalTarget: true) as FileInfo ?? file;
            if (!target.Exists)
            {
                return null;
            }

            return new DiscoveredFile(
                relativePath,
                file.Name,
                kind,
                target.Length,
                TruncateToMicroseconds(new DateTimeOffset(target.LastWriteTimeUtc, TimeSpan.Zero)));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
