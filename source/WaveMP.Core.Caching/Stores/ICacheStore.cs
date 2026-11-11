namespace WaveMP.Core.Caching.Stores;

/// <summary>
/// The files of the cache under <c>{DATA_PATH}/cache</c>, by area. The file system is the only record: an index in
/// memory is loaded from the folder on start and holds size and last use of every file; the last use is written as the
/// time of the last change of the file at most every <see cref="Options.CacheOptions.UsageTouchInterval"/>, so the
/// order of the clean-up survives a restart. When the cache grows beyond the size limit of the settings, the files
/// used least recently are removed in the background. If the cache folder cannot be written, the store is unavailable
/// and caches nothing.
/// </summary>
public interface ICacheStore
{
    /// <summary>
    /// Whether the cache folder could be written on start.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Absolute path of the cache folder.
    /// </summary>
    string RootPath { get; }

    /// <summary>
    /// Absolute path of the folder of an area.
    /// </summary>
    string GetAreaPath(CacheArea area);

    /// <summary>
    /// Creates the folders, checks that they can be written, removes unfinished files and loads the index. Errors make
    /// the store unavailable; they are logged and never thrown.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a file of the cache for reading and marks it as used; <c>null</c> if it does not exist. The receiver
    /// disposes the stream.
    /// </summary>
    Stream? TryOpen(CacheArea area, string name);

    /// <summary>
    /// Whether the cache has the file, without marking it as used.
    /// </summary>
    bool Contains(CacheArea area, string name);

    /// <summary>
    /// A new path for an unfinished file in the folder <c>tmp</c> of the cache, e.g. for a conversion.
    /// </summary>
    string CreateTemporaryPath(string extension);

    /// <summary>
    /// Moves a finished temporary file into the area under its name, replacing an existing file, and records it as
    /// used. Returns <c>false</c> (and removes the temporary file) if the store is unavailable or the file cannot be
    /// moved. Starts the clean-up in the background if the cache is now larger than its limit.
    /// </summary>
    bool Commit(CacheArea area, string name, string temporaryPath);

    /// <summary>
    /// Removes a temporary file; missing files are fine.
    /// </summary>
    void DeleteTemporary(string temporaryPath);

    CacheUsage GetUsage();

    /// <summary>
    /// Removes the files of an area whose name matches. Files that cannot be removed (e.g. because they are open) are
    /// skipped. Returns the number of removed files.
    /// </summary>
    Task<int> RemoveAsync(CacheArea area, Func<string, bool> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the files used least recently while the cache is larger than its limit, until it is at most the target
    /// share of the limit. Returns the number of removed files.
    /// </summary>
    Task<int> TrimAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts <see cref="TrimAsync"/> in the background unless it already runs; returns at once.
    /// </summary>
    void ScheduleTrim();

    /// <summary>
    /// Removes all files of all areas. Unfinished files stay, conversions may be writing them.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}
