using WaveMP.Data.Entities;

namespace WaveMP.Core.Scanning.Writers;

/// <summary>
/// Versions of what the scan stores, per kind of file. Raising the version of a kind makes the next scan read every
/// file of that kind again, e.g. when a later version stores more of a file.
/// </summary>
public static class ScannerVersions
{
    /// <summary>
    /// Version of the metadata of audio files.
    /// </summary>
    public const int AudioMetadata = 1;

    /// <summary>
    /// Version of the metadata of videos. Version 2 adds the codec of the audio stream and the pixel format, which the
    /// decision between direct play and transcoding needs.
    /// </summary>
    public const int VideoMetadata = 2;

    /// <summary>
    /// Version of the metadata of pictures.
    /// </summary>
    public const int ImageMetadata = 1;

    /// <summary>
    /// The current version of the metadata of a kind of file.
    /// </summary>
    public static int MetadataOf(MediaFileKind kind)
    {
        return kind switch
        {
            MediaFileKind.Video => VideoMetadata,
            MediaFileKind.Image => ImageMetadata,
            _ => AudioMetadata,
        };
    }
}
