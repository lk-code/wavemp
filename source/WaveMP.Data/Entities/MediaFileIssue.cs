namespace WaveMP.Data.Entities;

/// <summary>
/// A problem found while reading a media file. The file stays in the library. Stored as its name.
/// </summary>
public enum MediaFileIssue
{
    /// <summary>
    /// The tags cannot be read; ffprobe could read the file.
    /// </summary>
    TagsUnreadable,

    /// <summary>
    /// ffprobe cannot read the file; its tags could be read.
    /// </summary>
    ProbeFailed,

    /// <summary>
    /// The file cannot be read at all.
    /// </summary>
    Unreadable,
}
