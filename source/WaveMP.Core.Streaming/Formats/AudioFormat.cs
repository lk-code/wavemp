namespace WaveMP.Core.Streaming.Formats;

/// <summary>
/// An audio format as clients check it: a container together with its codec. Browsers decide by the MIME type with
/// its codecs (for example <c>audio/ogg; codecs="opus"</c>), so the container alone is not enough.
/// </summary>
public enum AudioFormat
{
    /// <summary>
    /// MP3 (MPEG-1/2 Audio Layer III).
    /// </summary>
    Mp3,

    /// <summary>
    /// FLAC in its own container.
    /// </summary>
    Flac,

    /// <summary>
    /// FLAC in an Ogg container.
    /// </summary>
    OggFlac,

    /// <summary>
    /// AAC in an ADTS stream (<c>.aac</c>).
    /// </summary>
    AdtsAac,

    /// <summary>
    /// AAC in an MPEG-4 container (<c>.m4a</c>, <c>.mp4</c>).
    /// </summary>
    Mp4Aac,

    /// <summary>
    /// Apple Lossless (ALAC) in an MPEG-4 container.
    /// </summary>
    Mp4Alac,

    /// <summary>
    /// Opus in an Ogg container (<c>.opus</c>, <c>.ogg</c>).
    /// </summary>
    OggOpus,

    /// <summary>
    /// Vorbis in an Ogg container (<c>.ogg</c>, <c>.oga</c>).
    /// </summary>
    OggVorbis,

    /// <summary>
    /// Opus in a WebM container.
    /// </summary>
    WebmOpus,

    /// <summary>
    /// Vorbis in a WebM container.
    /// </summary>
    WebmVorbis,

    /// <summary>
    /// Uncompressed PCM in a WAV container.
    /// </summary>
    WavPcm,

    /// <summary>
    /// Uncompressed PCM in an AIFF container.
    /// </summary>
    AiffPcm,
}
