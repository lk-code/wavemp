using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Machine-readable reason why a track has to be transcoded. Clients translate it.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<TranscodeReasonDto>))]
public enum TranscodeReasonDto
{
    /// <summary>
    /// The client does not support the format of the file.
    /// </summary>
    [JsonStringEnumMemberName("format_unsupported")]
    FormatUnsupported,

    /// <summary>
    /// The format of the file is none of the formats clients play directly, for example WMA or APE, or it is not known
    /// yet (a video read by an older version of the scan).
    /// </summary>
    [JsonStringEnumMemberName("format_unknown")]
    FormatUnknown,

    /// <summary>
    /// The client does not support the container of the video.
    /// </summary>
    [JsonStringEnumMemberName("container_unsupported")]
    ContainerUnsupported,

    /// <summary>
    /// The client does not support the codec of the picture of the video, or its variant (for example H.264 with
    /// 10 bit).
    /// </summary>
    [JsonStringEnumMemberName("video_codec_unsupported")]
    VideoCodecUnsupported,

    /// <summary>
    /// The client does not support the codec of the sound of the video.
    /// </summary>
    [JsonStringEnumMemberName("audio_codec_unsupported")]
    AudioCodecUnsupported,

    /// <summary>
    /// The track has no audio file; its sound comes from its video.
    /// </summary>
    [JsonStringEnumMemberName("audio_from_video")]
    AudioFromVideo,
}
