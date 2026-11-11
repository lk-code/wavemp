using WaveMP.Data.Entities;

namespace WaveMP.Core.Scanning.Discoveries;

/// <summary>
/// A media file found on disk.
/// </summary>
/// <param name="RelativePath">Path relative to the media folder with <c>/</c> as separator.</param>
/// <param name="FileName">Name with extension.</param>
/// <param name="Kind">Category after the extension.</param>
/// <param name="Size">Size in bytes.</param>
/// <param name="ModifiedAt">Time of the last change, truncated to microseconds.</param>
public sealed record DiscoveredFile(
    string RelativePath,
    string FileName,
    MediaFileKind Kind,
    long Size,
    DateTimeOffset ModifiedAt);
