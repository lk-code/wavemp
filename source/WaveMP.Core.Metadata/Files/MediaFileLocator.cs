using Microsoft.Extensions.Options;
using WaveMP.Core.Metadata.Options;

namespace WaveMP.Core.Metadata.Files;

/// <summary>
/// Resolves relative paths against the media folder of <see cref="MediaLibraryOptions"/>. The result always lies
/// inside the media folder, so a stored path can never point to other files of the server.
/// </summary>
internal sealed class MediaFileLocator(IOptions<MediaLibraryOptions> options) : IMediaFileLocator
{
    private static readonly StringComparison _pathComparison = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    public bool TryGetFullPath(string relativePath, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrEmpty(relativePath) || relativePath.Contains('\0', StringComparison.Ordinal))
        {
            return false;
        }

        string native = relativePath.Replace('/', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(native))
        {
            return false;
        }

        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.Value.RootPath));
        string candidate = Path.GetFullPath(Path.Combine(root, native));
        if (!candidate.StartsWith(root + Path.DirectorySeparatorChar, _pathComparison))
        {
            return false;
        }

        fullPath = candidate;
        return true;
    }
}
