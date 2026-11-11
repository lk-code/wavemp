using WaveMP.Data.Entities;

namespace WaveMP.Core.Metadata.Tags;

/// <summary>
/// Writes the tags of audio files; videos and pictures are never changed. Never throws for damaged, unsupported or
/// read-only files.
/// </summary>
public interface ITagWriter
{
    /// <summary>
    /// Reads the raw values of the fields that <see cref="Write"/> can change.
    /// </summary>
    TagSnapshotResult ReadSnapshot(string path);

    /// <summary>
    /// Writes the changes into the tags of an audio file. Fields without a change, sort fields and embedded pictures
    /// stay as they are; no new kinds of tags are added, except the native tag of the format if it is missing.
    /// </summary>
    TagWriteStatus Write(string path, MediaFileKind kind, TagChanges changes);
}
