using WaveMP.Data.Entities;

namespace WaveMP.Data.Repositories;

/// <summary>
/// Data access of the metadata editor.
/// </summary>
public interface ITrackMetadataRepository
{
    /// <summary>
    /// The track with its primary audio file, or <c>null</c> if the track does not exist.
    /// </summary>
    Task<TrackEditTarget?> FindEditTargetAsync(Guid trackId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a backup of tags at once.
    /// </summary>
    Task AddTagBackupAsync(TagBackup backup, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores whether the tags of a file can be written.
    /// </summary>
    Task SetWritableAsync(Guid mediaFileId, bool isWritable, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gives the artist with this normalized name the spelling <paramref name="name"/>. Returns the number of changed
    /// artists: 0 if there is no such artist or it already has this spelling, otherwise 1.
    /// </summary>
    Task<int> RenameArtistAsync(string normalizedName, string name, CancellationToken cancellationToken = default);
}
