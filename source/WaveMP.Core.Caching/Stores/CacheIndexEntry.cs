namespace WaveMP.Core.Caching.Stores;

/// <summary>
/// A file of the cache as the store knows it.
/// </summary>
/// <param name="Area">Area of the file.</param>
/// <param name="Name">Path of the file relative to the folder of its area, with <c>/</c> as separator.</param>
/// <param name="Size">Size in bytes.</param>
/// <param name="LastUsedAt">Last time the file was created or delivered.</param>
/// <param name="PersistedUseAt">Last use that was written as the time of the last change of the file.</param>
internal sealed record CacheIndexEntry(
    CacheArea Area,
    string Name,
    long Size,
    DateTimeOffset LastUsedAt,
    DateTimeOffset PersistedUseAt);
