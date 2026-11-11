using WaveMP.Core.Settings.Groups;
using WaveMP.Data.Entities;

namespace WaveMP.Core.Scanning.Discoveries;

/// <summary>
/// Classifies files as audio, video or picture by their extension, after the lists of the library settings.
/// </summary>
public sealed class MediaKindClassifier
{
    private readonly Dictionary<string, MediaFileKind> _kinds = new(StringComparer.OrdinalIgnoreCase);

    public MediaKindClassifier(LibrarySettings settings)
    {
        Add(settings.ImageExtensions, MediaFileKind.Image);
        Add(settings.VideoExtensions, MediaFileKind.Video);
        Add(settings.AudioExtensions, MediaFileKind.Audio);
    }

    /// <summary>
    /// The category of the file, or <c>null</c> if the library does not use files with its extension.
    /// </summary>
    public MediaFileKind? Classify(string fileName)
    {
        string extension = ExtensionOf(fileName);
        return extension.Length > 0 && _kinds.TryGetValue(extension, out MediaFileKind kind) ? kind : null;
    }

    /// <summary>
    /// The extension in lower case without the dot; empty for names without one.
    /// </summary>
    public static string ExtensionOf(string fileName)
    {
        int dot = fileName.LastIndexOf('.');
        return dot <= 0 || dot == fileName.Length - 1 ? string.Empty : fileName[(dot + 1)..].ToLowerInvariant();
    }

    /// <summary>
    /// The file name without its extension.
    /// </summary>
    public static string BaseNameOf(string fileName)
    {
        int dot = fileName.LastIndexOf('.');
        return dot <= 0 ? fileName : fileName[..dot];
    }

    private void Add(IEnumerable<string> extensions, MediaFileKind kind)
    {
        foreach (string extension in extensions)
        {
            _kinds[extension] = kind;
        }
    }
}
