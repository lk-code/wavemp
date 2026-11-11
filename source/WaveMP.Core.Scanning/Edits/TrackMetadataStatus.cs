namespace WaveMP.Core.Scanning.Edits;

/// <summary>
/// Whether the metadata editor can write the tags of a track.
/// </summary>
public enum TrackMetadataStatus
{
    /// <summary>
    /// The track has an audio file whose tags can be written.
    /// </summary>
    Editable,

    /// <summary>
    /// The track has a video only; tags are only written to audio files, never to videos.
    /// </summary>
    NoAudio,

    /// <summary>
    /// The audio file cannot be written: read-only file or media folder, missing permissions, unreadable tags or a
    /// missing file.
    /// </summary>
    NotWritable,
}
