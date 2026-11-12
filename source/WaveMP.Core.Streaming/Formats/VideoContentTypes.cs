namespace WaveMP.Core.Streaming.Formats;

/// <summary>
/// The MIME types (without codecs) videos, HLS playlists and HLS segments are delivered with.
/// </summary>
public static class VideoContentTypes
{
    public const string Mp4 = "video/mp4";
    public const string QuickTime = "video/quicktime";
    public const string Webm = "video/webm";
    public const string Matroska = "video/x-matroska";

    /// <summary>
    /// MIME type of videos whose container clients cannot play directly.
    /// </summary>
    public const string Unknown = "application/octet-stream";

    /// <summary>
    /// HLS playlists (master and media playlists).
    /// </summary>
    public const string HlsPlaylist = "application/vnd.apple.mpegurl";

    /// <summary>
    /// HLS segments in an MPEG transport stream.
    /// </summary>
    public const string MpegTransportStream = "video/mp2t";
}
