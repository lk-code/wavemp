using WaveMP.Core.Scanning.Discoveries;
using WaveMP.Core.Scanning.Groupings;
using WaveMP.Data.Entities;

namespace WaveMP.Core.Scanning.Tests;

/// <summary>
/// Media file entities for tests that need no files on disk.
/// </summary>
internal static class TestFiles
{
    public static MediaFile Create(
        string relativePath,
        MediaFileKind? kind = null,
        Action<MediaFile>? configure = null)
    {
        int slash = relativePath.LastIndexOf('/');
        string fileName = slash < 0 ? relativePath : relativePath[(slash + 1)..];
        string extension = MediaKindClassifier.ExtensionOf(fileName);
        string baseName = MediaKindClassifier.BaseNameOf(fileName);
        MediaFile file = new()
        {
            Id = Guid.NewGuid(),
            RelativePath = relativePath,
            DirectoryPath = slash < 0 ? string.Empty : relativePath[..slash],
            FileName = fileName,
            BaseName = baseName,
            NormalizedBaseName = FileGrouper.NormalizeBaseName(baseName),
            Extension = extension,
            Kind = kind ?? extension switch
            {
                "mkv" or "mp4" => MediaFileKind.Video,
                "jpg" or "png" => MediaFileKind.Image,
                _ => MediaFileKind.Audio,
            },
            Size = 1000,
            ModifiedAt = DateTimeOffset.UnixEpoch,
            IsPrimary = false,
            IsLossless = false,
            HasEmbeddedPicture = false,
            IsWritable = false,
            MetadataVersion = 1,
            FirstSeenAt = DateTimeOffset.UnixEpoch,
            ScannedAt = DateTimeOffset.UnixEpoch,
        };
        configure?.Invoke(file);
        return file;
    }
}
