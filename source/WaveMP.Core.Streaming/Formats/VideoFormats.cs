namespace WaveMP.Core.Streaming.Formats;

/// <summary>
/// Assigns videos of the library to the containers and codecs clients check, by what ffprobe reported during the scan.
/// Pure functions.
/// </summary>
public static class VideoFormats
{
    /// <summary>
    /// The version of the metadata of videos from which the scan stores the codec of the sound and the pixel format
    /// (the version of videos of the scan). Videos read by an older version are transcoded until they are read again.
    /// </summary>
    public const int MetadataVersion = 2;

    private static readonly HashSet<string> _eightBit420 = new(["yuv420p", "yuvj420p"], StringComparer.Ordinal);

    /// <summary>
    /// The container of a video from the container format of ffprobe (<c>format_name</c>, e.g.
    /// <c>mov,mp4,m4a,3gp,3g2,mj2</c> or <c>matroska,webm</c>) and its extension; ffprobe names MP4 and QuickTime the
    /// same and Matroska and WebM as well, so the extension decides. <c>null</c> for other containers (e.g. AVI or
    /// MPEG-TS) and files without probe data.
    /// </summary>
    public static VideoContainer? ContainerOf(string? format, string? extension)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return null;
        }

        string[] containers = format.ToLowerInvariant().Split(',', StringSplitOptions.TrimEntries);
        string fileExtension = extension?.Trim().TrimStart('.').ToLowerInvariant() ?? string.Empty;
        if (Has(containers, "mp4") || Has(containers, "mov"))
        {
            return fileExtension switch
            {
                "mp4" or "m4v" => VideoContainer.Mp4,
                "mov" => VideoContainer.Mov,
                _ => null,
            };
        }

        if (Has(containers, "matroska") || Has(containers, "webm"))
        {
            return fileExtension switch
            {
                "webm" => VideoContainer.Webm,
                "mkv" => VideoContainer.Matroska,
                _ => null,
            };
        }

        return null;
    }

    /// <summary>
    /// The codec of a video stream from its ffprobe <c>codec_name</c>; <c>null</c> for codecs clients cannot play
    /// directly (e.g. MPEG-4 part 2 or MPEG-2).
    /// </summary>
    public static VideoCodec? VideoCodecOf(string? codec)
    {
        return codec?.Trim().ToLowerInvariant() switch
        {
            "h264" => VideoCodec.H264,
            "hevc" => VideoCodec.Hevc,
            "vp8" => VideoCodec.Vp8,
            "vp9" => VideoCodec.Vp9,
            "av1" => VideoCodec.Av1,
            _ => null,
        };
    }

    /// <summary>
    /// The codec of the sound of a video from its ffprobe <c>codec_name</c>; <c>null</c> for codecs clients cannot
    /// play directly (e.g. DTS or PCM).
    /// </summary>
    public static AudioCodec? AudioCodecOf(string? codec)
    {
        return codec?.Trim().ToLowerInvariant() switch
        {
            "aac" => AudioCodec.Aac,
            "mp3" => AudioCodec.Mp3,
            "opus" => AudioCodec.Opus,
            "vorbis" => AudioCodec.Vorbis,
            "flac" => AudioCodec.Flac,
            "ac3" => AudioCodec.Ac3,
            "eac3" => AudioCodec.Eac3,
            _ => null,
        };
    }

    /// <summary>
    /// Whether the pixel format is 8 bit with 4:2:0 chroma, the only H.264 variant every browser decodes.
    /// </summary>
    public static bool IsEightBit420(string? pixelFormat)
    {
        return pixelFormat is not null && _eightBit420.Contains(pixelFormat.Trim().ToLowerInvariant());
    }

    /// <summary>
    /// The MIME type a video of the container is delivered with; <see cref="VideoContentTypes.Unknown"/> without one.
    /// </summary>
    public static string ContentTypeOf(VideoContainer? container)
    {
        return container switch
        {
            VideoContainer.Mp4 => VideoContentTypes.Mp4,
            VideoContainer.Mov => VideoContentTypes.QuickTime,
            VideoContainer.Webm => VideoContentTypes.Webm,
            VideoContainer.Matroska => VideoContentTypes.Matroska,
            _ => VideoContentTypes.Unknown,
        };
    }

    private static bool Has(string[] containers, string name)
    {
        return Array.IndexOf(containers, name) >= 0;
    }
}
