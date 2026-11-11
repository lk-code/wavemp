using WaveMP.Data.Entities;

namespace WaveMP.Data.Repositories;

/// <summary>
/// Writes the library during a scan. Changes of a folder are collected on tracked entities and saved together with
/// <see cref="SaveChangesAsync"/>, so every folder is updated atomically.
/// </summary>
public interface ILibraryScanRepository
{
    /// <summary>
    /// The stored files in the folders and all their subfolders; <c>null</c> for the whole library.
    /// </summary>
    Task<IReadOnlyList<MediaFileSnapshot>> GetFileSnapshotsAsync(
        IReadOnlyCollection<string>? directories,
        CancellationToken cancellationToken = default);

    Task<int> CountFilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The files and tracks of one folder (not its subfolders), tracked.
    /// </summary>
    Task<StoredFolder> LoadFolderAsync(string directoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// An artist by its normalized name, tracked.
    /// </summary>
    Task<Artist?> FindArtistAsync(string normalizedName, CancellationToken cancellationToken = default);

    /// <summary>
    /// An album by its key, tracked.
    /// </summary>
    Task<Album?> FindAlbumAsync(string albumKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new library entity; saved with <see cref="SaveChangesAsync"/>.
    /// </summary>
    void Add<TEntity>(TEntity entity)
        where TEntity : class;

    /// <summary>
    /// Removes a tracked library entity; saved with <see cref="SaveChangesAsync"/>.
    /// </summary>
    void Remove<TEntity>(TEntity entity)
        where TEntity : class;

    /// <summary>
    /// Saves all collected changes in one transaction and stops tracking the entities.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the files and tracks of folders that no longer exist (not their subfolders).
    /// </summary>
    Task DeleteDirectoriesAsync(IReadOnlyCollection<string> directories, CancellationToken cancellationToken = default);

    /// <summary>
    /// The folders that contain tracks of an album.
    /// </summary>
    Task<IReadOnlyList<AlbumFolder>> GetAlbumFoldersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The pictures whose normalized base name is one of <paramref name="normalizedBaseNames"/>.
    /// </summary>
    Task<IReadOnlyList<AlbumCoverImage>> GetAlbumCoverImagesAsync(
        IReadOnlyCollection<string> normalizedBaseNames,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The covers of all albums, tracked.
    /// </summary>
    Task<IReadOnlyList<Cover>> LoadAlbumCoversAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes albums without tracks and artists without tracks and albums. Returns the number of deleted albums and
    /// artists.
    /// </summary>
    Task<(int Albums, int Artists)> DeleteOrphansAsync(CancellationToken cancellationToken = default);

    Task<LibraryTotals> CountLibraryAsync(CancellationToken cancellationToken = default);
}
