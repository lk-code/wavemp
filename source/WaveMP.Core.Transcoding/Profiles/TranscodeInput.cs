namespace WaveMP.Core.Transcoding.Profiles;

/// <summary>
/// The file a transcode reads.
/// </summary>
/// <param name="TrackId">Id of the track.</param>
/// <param name="FileId">Id of the file in the library.</param>
/// <param name="FullPath">Absolute path of the file.</param>
/// <param name="Length">Size of the file in bytes; part of the key of the transcode.</param>
/// <param name="LastModified">Time of the last change of the file; part of the key of the transcode.</param>
/// <param name="Duration">Duration of the file.</param>
/// <param name="HasAudio">Whether the file has sound.</param>
/// <param name="Width">Width of the picture, if known and the file is a video.</param>
/// <param name="Height">Height of the picture, if known and the file is a video.</param>
public sealed record TranscodeInput(
    Guid TrackId,
    Guid FileId,
    string FullPath,
    long Length,
    DateTimeOffset LastModified,
    TimeSpan Duration,
    bool HasAudio,
    int? Width = null,
    int? Height = null);
