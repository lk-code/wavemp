namespace WaveMP.Core.Streaming.Formats;

/// <summary>
/// The MIME types (without codecs) audio files are delivered with, see <see cref="AudioFormats.ContentTypeOf"/>.
/// </summary>
public static class AudioContentTypes
{
    public const string Mpeg = "audio/mpeg";
    public const string Flac = "audio/flac";
    public const string Ogg = "audio/ogg";
    public const string Aac = "audio/aac";
    public const string Mp4 = "audio/mp4";
    public const string Webm = "audio/webm";
    public const string Wav = "audio/wav";
    public const string Aiff = "audio/aiff";

    /// <summary>
    /// MIME type of files whose format clients cannot play directly.
    /// </summary>
    public const string Unknown = "application/octet-stream";
}
