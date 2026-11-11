namespace WaveMP.Core.Playlists.Covers;

/// <summary>
/// The uploaded covers of the playlists in the folder <c>playlists</c> of the data folder: one folder per playlist
/// with the cover as square WebP pictures of 96, 300 and 1000 pixels (never enlarged). Unlike the cache, the folder
/// holds data of the users and belongs into backups.
/// </summary>
public interface IPlaylistCoverStore
{
    /// <summary>
    /// Whether covers can be stored; <c>false</c> if the folder cannot be written.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Creates the folder and removes unfinished files. A folder that cannot be written does not fail; the reason is
    /// logged and uploads are refused.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Recognizes the picture by its content (JPEG, PNG or WebP), crops it to a square around its center and stores
    /// it in every size, replacing an earlier cover of the playlist.
    /// </summary>
    Task<CoverSaveStatus> SaveAsync(Guid playlistId, Stream content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens the stored picture of the size (see <see cref="Caching.Covers.CoverSizes.Snap"/>; the largest without a
    /// size), or <c>null</c> if there is none. The caller disposes it.
    /// </summary>
    Stream? Open(Guid playlistId, int size);

    /// <summary>
    /// Removes the cover of a playlist; a playlist without one is fine.
    /// </summary>
    void Delete(Guid playlistId);

    /// <summary>
    /// The ids of the playlists that have a folder in the store.
    /// </summary>
    IReadOnlyList<Guid> ListPlaylistIds();
}
