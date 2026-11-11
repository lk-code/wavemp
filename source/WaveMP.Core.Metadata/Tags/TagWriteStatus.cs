namespace WaveMP.Core.Metadata.Tags;

/// <summary>
/// Outcome of writing the tags of a file.
/// </summary>
public enum TagWriteStatus
{
    /// <summary>
    /// The tags were written, or there was nothing to change.
    /// </summary>
    Written,

    /// <summary>
    /// The file is no audio file; tags are only written to audio files, never to videos. The file was not opened.
    /// </summary>
    NotAudio,

    /// <summary>
    /// The tag library cannot write tags of this format.
    /// </summary>
    Unsupported,

    /// <summary>
    /// The file is damaged; nothing was written.
    /// </summary>
    Unreadable,

    /// <summary>
    /// The file cannot be opened for writing: read-only, without permission, on a read-only mount or missing.
    /// </summary>
    NotWritable,
}
