namespace WaveMP.Core.Metadata.Codecs;

/// <summary>
/// Knows the lossless audio codecs by their ffprobe names.
/// </summary>
public static class LosslessCodecs
{
    private static readonly HashSet<string> _codecs = new(StringComparer.OrdinalIgnoreCase)
    {
        "flac",
        "alac",
        "ape",
        "wavpack",
        "tta",
        "tak",
        "mlp",
        "truehd",
        "wmalossless",
        "shorten",
    };

    public static bool IsLossless(string? codec)
    {
        return codec is not null
            && (_codecs.Contains(codec)
                || codec.StartsWith("pcm_", StringComparison.OrdinalIgnoreCase)
                || codec.StartsWith("dsd_", StringComparison.OrdinalIgnoreCase));
    }
}
