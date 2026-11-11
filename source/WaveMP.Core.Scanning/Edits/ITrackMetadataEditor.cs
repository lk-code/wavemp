using WaveMP.Core.Metadata.Tags;

namespace WaveMP.Core.Scanning.Edits;

/// <summary>
/// Edits the metadata of a track in the tags of its audio file. Only the primary audio file is written (the one the
/// track plays and takes its metadata from), never videos or other files; file names stay as they are.
/// </summary>
public interface ITrackMetadataEditor
{
    /// <summary>
    /// The metadata of the track and whether its tags can be written, or <c>null</c> if the track does not exist.
    /// Checks the audio file each time, so a media folder mounted writable later counts without a scan.
    /// </summary>
    Task<TrackMetadata?> GetAsync(Guid trackId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the current tags of the audio file in the database, writes the changes into the file and reads the
    /// folder of the track again. Texts are normalized; empty texts and numbers below 1 remove the field.
    /// </summary>
    Task<TrackMetadataResult> UpdateAsync(
        Guid trackId,
        TagChanges changes,
        CancellationToken cancellationToken = default);
}
