namespace WaveMP.Core.Caching.Stores;

/// <summary>
/// The areas of the cache, each in a folder of its own. The size limit and the clean-up apply to all areas together.
/// </summary>
public enum CacheArea
{
    /// <summary>
    /// Scaled covers (folder <c>covers</c>).
    /// </summary>
    Covers,

    /// <summary>
    /// Transcoded media (folder <c>transcodes</c>); reserved for the transcoding.
    /// </summary>
    Transcodes,
}
