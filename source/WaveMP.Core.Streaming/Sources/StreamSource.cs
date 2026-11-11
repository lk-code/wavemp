using WaveMP.Core.Streaming.Formats;

namespace WaveMP.Core.Streaming.Sources;

/// <summary>
/// The audio file a track plays, as found on the disk right now.
/// </summary>
/// <param name="TrackId">Id of the track.</param>
/// <param name="FullPath">Absolute path of the file; always inside the media folder.</param>
/// <param name="Format">Format of the file; <c>null</c> if clients cannot play it directly.</param>
/// <param name="ContentType">MIME type the file is delivered with.</param>
/// <param name="Length">Size of the file in bytes.</param>
/// <param name="LastModified">Time of the last change of the file, in whole seconds (HTTP precision).</param>
/// <param name="ETag">Strong entity tag (without quotes) of this version of the file.</param>
/// <param name="Duration">Duration of the file, if known.</param>
/// <param name="ReplayGain">The ReplayGain tags of the file.</param>
public sealed record StreamSource(
    Guid TrackId,
    string FullPath,
    AudioFormat? Format,
    string ContentType,
    long Length,
    DateTimeOffset LastModified,
    string ETag,
    TimeSpan? Duration,
    ReplayGainValues ReplayGain);
