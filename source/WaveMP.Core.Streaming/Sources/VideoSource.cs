using WaveMP.Core.Streaming.Formats;

namespace WaveMP.Core.Streaming.Sources;

/// <summary>
/// The video a track plays, as found on the disk right now.
/// </summary>
/// <param name="TrackId">Id of the track.</param>
/// <param name="FileId">Id of the file in the library.</param>
/// <param name="FullPath">Absolute path of the file; always inside the media folder.</param>
/// <param name="Container">Container of the file; <c>null</c> if clients cannot play it directly.</param>
/// <param name="VideoCodec">Codec of the picture; <c>null</c> if clients cannot play it directly.</param>
/// <param name="AudioCodec">Codec of the sound; <c>null</c> without sound or if clients cannot play it directly.</param>
/// <param name="HasAudio">Whether the video has sound.</param>
/// <param name="PixelFormat">Pixel format of the picture, e.g. <c>yuv420p</c>.</param>
/// <param name="Width">Width of the picture, if known.</param>
/// <param name="Height">Height of the picture, if known.</param>
/// <param name="SampleRate">Sample rate of the sound, if known.</param>
/// <param name="ContentType">MIME type the file is delivered with.</param>
/// <param name="Length">Size of the file in bytes.</param>
/// <param name="LastModified">Time of the last change of the file, in whole seconds (HTTP precision).</param>
/// <param name="ETag">Strong entity tag (without quotes) of this version of the file.</param>
/// <param name="Duration">Duration of the file, if known.</param>
/// <param name="MetadataComplete">Whether the scan stored everything the decision needs (sound and pixel format);
/// videos read by an older scan are transcoded until the next scan reads them again.</param>
public sealed record VideoSource(
    Guid TrackId,
    Guid FileId,
    string FullPath,
    VideoContainer? Container,
    VideoCodec? VideoCodec,
    AudioCodec? AudioCodec,
    bool HasAudio,
    string? PixelFormat,
    int? Width,
    int? Height,
    int? SampleRate,
    string ContentType,
    long Length,
    DateTimeOffset LastModified,
    string ETag,
    TimeSpan? Duration,
    bool MetadataComplete);
