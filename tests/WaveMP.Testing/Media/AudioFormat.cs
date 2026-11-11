namespace WaveMP.Testing.Media;

/// <summary>
/// Audio formats of the synthetic test media; the file extension is the lower-case name.
/// </summary>
public enum AudioFormat
{
    /// <summary>
    /// MP3 (libmp3lame), lossy.
    /// </summary>
    Mp3,

    /// <summary>
    /// FLAC, lossless.
    /// </summary>
    Flac,

    /// <summary>
    /// AAC in an MP4 container, lossy.
    /// </summary>
    M4a,

    /// <summary>
    /// Opus in an Ogg container, lossy.
    /// </summary>
    Opus,

    /// <summary>
    /// PCM in a WAV container, lossless.
    /// </summary>
    Wav,
}
