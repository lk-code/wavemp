namespace WaveMP.Core.Streaming.Formats;

/// <summary>
/// A codec of the sound of videos that clients may play directly.
/// </summary>
public enum AudioCodec
{
    Aac,

    Mp3,

    Opus,

    Vorbis,

    Flac,

    /// <summary>
    /// Dolby Digital (AC-3).
    /// </summary>
    Ac3,

    /// <summary>
    /// Dolby Digital Plus (E-AC-3).
    /// </summary>
    Eac3,
}
