namespace WaveMP.Core.Streaming.Formats;

/// <summary>
/// Assigns the audio files of the library to an <see cref="AudioFormat"/> by the container and codec that ffprobe
/// reported during the scan, and names the MIME type each format is delivered with.
/// </summary>
public static class AudioFormats
{
    /// <summary>
    /// MIME type of files whose format clients cannot play directly.
    /// </summary>
    public const string UnknownContentType = "application/octet-stream";

    private static readonly HashSet<string> _wavCodecs =
        new(["pcm_u8", "pcm_s16le", "pcm_s24le", "pcm_s32le", "pcm_f32le"], StringComparer.Ordinal);

    private static readonly HashSet<string> _aiffCodecs =
        new(["pcm_s8", "pcm_s16be", "pcm_s24be", "pcm_s32be", "pcm_f32be"], StringComparer.Ordinal);

    /// <summary>
    /// The format of a file from the container (ffprobe <c>format_name</c>, for example <c>ogg</c> or
    /// <c>mov,mp4,m4a,3gp,3g2,mj2</c>), the codec of its audio stream (<c>codec_name</c>) and its extension. Returns
    /// <c>null</c> for formats clients cannot play directly (for example WMA, APE, WavPack, Musepack, DSD or Matroska
    /// audio) and for files without probe data; they have to be transcoded.
    /// </summary>
    public static AudioFormat? FromFile(string? format, string? codec, string? extension)
    {
        if (string.IsNullOrWhiteSpace(format) || string.IsNullOrWhiteSpace(codec))
        {
            return null;
        }

        string[] containers = format.ToLowerInvariant().Split(',', StringSplitOptions.TrimEntries);
        string codecName = codec.Trim().ToLowerInvariant();
        string fileExtension = extension?.Trim().TrimStart('.').ToLowerInvariant() ?? string.Empty;

        if (Has(containers, "mp3"))
        {
            return codecName == "mp3" ? AudioFormat.Mp3 : null;
        }

        if (Has(containers, "flac"))
        {
            return codecName == "flac" ? AudioFormat.Flac : null;
        }

        if (Has(containers, "ogg"))
        {
            return codecName switch
            {
                "flac" => AudioFormat.OggFlac,
                "opus" => AudioFormat.OggOpus,
                "vorbis" => AudioFormat.OggVorbis,
                _ => null,
            };
        }

        if (Has(containers, "aac"))
        {
            return codecName == "aac" ? AudioFormat.AdtsAac : null;
        }

        if (Has(containers, "mp4") || Has(containers, "m4a") || Has(containers, "mov"))
        {
            return codecName switch
            {
                "aac" => AudioFormat.Mp4Aac,
                "alac" => AudioFormat.Mp4Alac,
                _ => null,
            };
        }

        // ffprobe names Matroska and WebM the same; browsers only play the WebM subset, so only .webm counts.
        if (Has(containers, "webm") && fileExtension == "webm")
        {
            return codecName switch
            {
                "opus" => AudioFormat.WebmOpus,
                "vorbis" => AudioFormat.WebmVorbis,
                _ => null,
            };
        }

        if (Has(containers, "wav"))
        {
            return _wavCodecs.Contains(codecName) ? AudioFormat.WavPcm : null;
        }

        if (Has(containers, "aiff"))
        {
            return _aiffCodecs.Contains(codecName) ? AudioFormat.AiffPcm : null;
        }

        return null;
    }

    /// <summary>
    /// The MIME type (without codecs) a file of the format is delivered with.
    /// </summary>
    public static string ContentTypeOf(AudioFormat format)
    {
        return format switch
        {
            AudioFormat.Mp3 => "audio/mpeg",
            AudioFormat.Flac => "audio/flac",
            AudioFormat.OggFlac or AudioFormat.OggOpus or AudioFormat.OggVorbis => "audio/ogg",
            AudioFormat.AdtsAac => "audio/aac",
            AudioFormat.Mp4Aac or AudioFormat.Mp4Alac => "audio/mp4",
            AudioFormat.WebmOpus or AudioFormat.WebmVorbis => "audio/webm",
            AudioFormat.WavPcm => "audio/wav",
            AudioFormat.AiffPcm => "audio/aiff",
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown audio format."),
        };
    }

    private static bool Has(string[] containers, string name)
    {
        return Array.IndexOf(containers, name) >= 0;
    }
}
