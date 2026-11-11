using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// An audio format as clients check it: a container together with its codec. A browser tests each one with
/// canPlayType and the MIME type with its codecs, for example <c>audio/ogg; codecs="opus"</c> for OggOpus.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<AudioFormatDto>))]
public enum AudioFormatDto
{
    /// <summary>
    /// MP3 (<c>audio/mpeg</c>).
    /// </summary>
    Mp3,

    /// <summary>
    /// FLAC in its own container (<c>audio/flac</c>).
    /// </summary>
    Flac,

    /// <summary>
    /// FLAC in an Ogg container (<c>audio/ogg; codecs="flac"</c>).
    /// </summary>
    OggFlac,

    /// <summary>
    /// AAC in an ADTS stream (<c>audio/aac</c>).
    /// </summary>
    AdtsAac,

    /// <summary>
    /// AAC in an MPEG-4 container (<c>audio/mp4; codecs="mp4a.40.2"</c>).
    /// </summary>
    Mp4Aac,

    /// <summary>
    /// Apple Lossless in an MPEG-4 container (<c>audio/mp4; codecs="alac"</c>).
    /// </summary>
    Mp4Alac,

    /// <summary>
    /// Opus in an Ogg container (<c>audio/ogg; codecs="opus"</c>).
    /// </summary>
    OggOpus,

    /// <summary>
    /// Vorbis in an Ogg container (<c>audio/ogg; codecs="vorbis"</c>).
    /// </summary>
    OggVorbis,

    /// <summary>
    /// Opus in a WebM container (<c>audio/webm; codecs="opus"</c>).
    /// </summary>
    WebmOpus,

    /// <summary>
    /// Vorbis in a WebM container (<c>audio/webm; codecs="vorbis"</c>).
    /// </summary>
    WebmVorbis,

    /// <summary>
    /// Uncompressed PCM in a WAV container (<c>audio/wav; codecs="1"</c>).
    /// </summary>
    WavPcm,

    /// <summary>
    /// Uncompressed PCM in an AIFF container (<c>audio/aiff</c>).
    /// </summary>
    AiffPcm,
}
