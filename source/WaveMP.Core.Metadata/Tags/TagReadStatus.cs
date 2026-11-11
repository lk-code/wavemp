namespace WaveMP.Core.Metadata.Tags;

/// <summary>
/// Outcome of reading the tags of a file.
/// </summary>
public enum TagReadStatus
{
    /// <summary>
    /// The tags were read (they may be empty).
    /// </summary>
    Read,

    /// <summary>
    /// The tag library does not support the format; not an error of the file.
    /// </summary>
    Unsupported,

    /// <summary>
    /// The file is damaged or cannot be opened.
    /// </summary>
    Unreadable,
}
