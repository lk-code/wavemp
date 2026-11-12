using System.Globalization;
using System.Text;
using WaveMP.Core.Transcoding.Profiles;

namespace WaveMP.Core.Transcoding.Playlists;

/// <summary>
/// Writes the HLS playlists of a variant. The media playlist is a complete VOD playlist computed from the duration, so
/// players can seek anywhere before a segment exists. The URIs are relative and carry the signed query of the request,
/// because players do not send headers. Pure functions; numbers are always written with the invariant culture.
/// </summary>
public static class HlsPlaylistWriter
{
    /// <summary>
    /// Content type of the playlists.
    /// </summary>
    public const string ContentType = "application/vnd.apple.mpegurl";

    /// <summary>
    /// Content type of the segments.
    /// </summary>
    public const string SegmentContentType = "video/mp2t";

    /// <summary>
    /// The master playlist with the one variant; <paramref name="mediaPlaylistUri"/> is relative to it.
    /// </summary>
    public static string WriteMaster(TranscodeVariant variant, string mediaPlaylistUri)
    {
        StringBuilder text = new();
        text.Append("#EXTM3U\n");
        text.Append("#EXT-X-VERSION:3\n");
        text.Append("#EXT-X-INDEPENDENT-SEGMENTS\n");
        text.Append(CultureInfo.InvariantCulture, $"#EXT-X-STREAM-INF:BANDWIDTH={variant.Bandwidth}");
        text.Append(CultureInfo.InvariantCulture, $",CODECS=\"{variant.Codecs}\"");
        if (variant.Media == HlsMedia.Video && variant.Profile.Size is { } size)
        {
            text.Append(CultureInfo.InvariantCulture, $",RESOLUTION={size.Width}x{size.Height}");
        }

        text.Append('\n');
        text.Append(mediaPlaylistUri).Append('\n');
        return text.ToString();
    }

    /// <summary>
    /// The VOD media playlist with all segments; <paramref name="segmentUriOf"/> gives the URI of a segment, relative to
    /// the playlist.
    /// </summary>
    public static string WriteMedia(TranscodeVariant variant, Func<int, string> segmentUriOf)
    {
        int count = variant.SegmentCount;
        double longest = Enumerable.Range(0, count).Max(index => variant.DurationOf(index).TotalSeconds);

        StringBuilder text = new();
        text.Append("#EXTM3U\n");
        text.Append("#EXT-X-VERSION:3\n");
        text.Append("#EXT-X-PLAYLIST-TYPE:VOD\n");
        text.Append("#EXT-X-INDEPENDENT-SEGMENTS\n");
        text.Append(CultureInfo.InvariantCulture, $"#EXT-X-TARGETDURATION:{(int)Math.Ceiling(longest)}\n");
        text.Append("#EXT-X-MEDIA-SEQUENCE:0\n");
        for (int index = 0; index < count; index++)
        {
            double seconds = variant.DurationOf(index).TotalSeconds;
            text.Append(CultureInfo.InvariantCulture, $"#EXTINF:{seconds:0.000},\n");
            text.Append(segmentUriOf(index)).Append('\n');
        }

        text.Append("#EXT-X-ENDLIST\n");
        return text.ToString();
    }

    /// <summary>
    /// The finished segments in a playlist ffmpeg writes: the index of every segment file it lists.
    /// </summary>
    public static IReadOnlyList<int> ReadSegmentIndexes(string playlist)
    {
        List<int> indexes = [];
        foreach (string line in playlist.Split('\n'))
        {
            string entry = line.Trim();
            if (entry.Length == 0 || entry.StartsWith('#'))
            {
                continue;
            }

            if (Arguments.FfmpegHlsArguments.SegmentIndexOf(entry) is { } index)
            {
                indexes.Add(index);
            }
        }

        return indexes;
    }
}
