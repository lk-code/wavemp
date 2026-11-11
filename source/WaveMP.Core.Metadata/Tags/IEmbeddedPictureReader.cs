namespace WaveMP.Core.Metadata.Tags;

/// <summary>
/// Reads the picture embedded in the tags of an audio or video file, e.g. to deliver it as cover.
/// </summary>
public interface IEmbeddedPictureReader
{
    /// <summary>
    /// Largest picture that is read, in bytes.
    /// </summary>
    const int MaxPictureSize = 32 * 1024 * 1024;

    /// <summary>
    /// The front cover embedded in the tags, otherwise the first picture. Returns <c>null</c> if the file has no
    /// picture, cannot be read or the picture is larger than <see cref="MaxPictureSize"/>. Never throws for damaged or
    /// missing files.
    /// </summary>
    EmbeddedPictureData? ReadPicture(string path);
}
