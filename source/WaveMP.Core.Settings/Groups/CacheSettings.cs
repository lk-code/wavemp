namespace WaveMP.Core.Settings.Groups;

/// <summary>
/// Settings of the cache under <c>/data/cache</c> (scaled covers and transcoded media). Stored as the settings
/// group <see cref="Key"/>; values missing in the stored JSON keep the defaults below.
/// </summary>
public sealed class CacheSettings
{
    /// <summary>
    /// Key of the settings group.
    /// </summary>
    public const string Key = "cache";

    /// <summary>
    /// Bytes of a megabyte as the settings count them (SI, like the sizes shown by the web app).
    /// </summary>
    public const long BytesPerMegabyte = 1_000_000;

    /// <summary>
    /// Largest size of the cache in megabytes. When the cache grows larger, the files used least recently are removed.
    /// </summary>
    public int MaxSizeMegabytes { get; init; } = 2000;

    /// <summary>
    /// <see cref="MaxSizeMegabytes"/> in bytes. A method, so that it is not stored with the settings.
    /// </summary>
    public long GetMaxSizeBytes()
    {
        return MaxSizeMegabytes * BytesPerMegabyte;
    }
}
