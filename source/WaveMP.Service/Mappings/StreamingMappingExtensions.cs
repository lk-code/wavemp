using WaveMP.Core.Streaming.Decisions;
using WaveMP.Core.Streaming.Formats;
using WaveMP.Core.Streaming.Sources;
using WaveMP.Service.Dtos;
using WaveMP.Service.Signatures;

namespace WaveMP.Service.Mappings;

public static class StreamingMappingExtensions
{
    /// <summary>
    /// The playback of a found track; <paramref name="stream"/> is the signed URL of its file (direct play) or of its
    /// HLS master playlist (transcoding), <c>null</c> if transcoding is unavailable.
    /// </summary>
    public static TrackPlaybackDto ToDto(
        this StreamSource source,
        PlaybackDecision decision,
        MediaUrl? stream)
    {
        return new TrackPlaybackDto
        {
            TrackId = source.TrackId,
            Method = decision.Method.ToDto(),
            Protocol = ProtocolOf(decision.Method),
            Format = decision.Format?.ToDto(),
            TranscodeReason = decision.Reason?.ToDto(),
            ContentType = ContentTypeOf(decision.Method, source.ContentType, stream),
            StreamUrl = stream?.Url,
            ExpiresAt = stream?.ExpiresAt,
            DurationMilliseconds = source.Duration is { } duration
                ? (long)Math.Round(duration.TotalMilliseconds)
                : null,
            ReplayGain = source.ReplayGain.ToDto(),
        };
    }

    /// <summary>
    /// The video playback of a found track; <paramref name="stream"/> is the signed URL of its file (direct play) or of
    /// its HLS master playlist (transcoding), <c>null</c> if transcoding is unavailable.
    /// </summary>
    public static TrackVideoPlaybackDto ToDto(
        this VideoSource source,
        VideoPlaybackDecision decision,
        MediaUrl? stream)
    {
        return new TrackVideoPlaybackDto
        {
            TrackId = source.TrackId,
            Method = decision.Method.ToDto(),
            Protocol = ProtocolOf(decision.Method),
            TranscodeReason = decision.Reason?.ToDto(),
            Container = source.Container?.ToDto(),
            VideoCodec = source.VideoCodec?.ToDto(),
            AudioCodec = source.AudioCodec?.ToDto(),
            HasAudio = source.HasAudio,
            Width = source.Width,
            Height = source.Height,
            ContentType = ContentTypeOf(decision.Method, source.ContentType, stream),
            StreamUrl = stream?.Url,
            ExpiresAt = stream?.ExpiresAt,
            DurationMilliseconds = source.Duration is { } duration
                ? (long)Math.Round(duration.TotalMilliseconds)
                : null,
        };
    }

    public static StreamProtocolDto ProtocolOf(PlaybackMethod method)
    {
        return method == PlaybackMethod.DirectPlay ? StreamProtocolDto.File : StreamProtocolDto.Hls;
    }

    public static VideoCapabilities ToCapabilities(
        VideoContainerDto[]? containers,
        VideoCodecDto[]? videoCodecs,
        AudioCodecDto[]? audioCodecs)
    {
        return new VideoCapabilities(
            (containers ?? []).Select(ToEntity).ToHashSet(),
            (videoCodecs ?? []).Select(ToEntity).ToHashSet(),
            (audioCodecs ?? []).Select(ToEntity).ToHashSet());
    }

    public static VideoContainerDto ToDto(this VideoContainer container)
    {
        return container switch
        {
            VideoContainer.Mp4 => VideoContainerDto.Mp4,
            VideoContainer.Mov => VideoContainerDto.Mov,
            VideoContainer.Webm => VideoContainerDto.Webm,
            VideoContainer.Matroska => VideoContainerDto.Matroska,
            _ => throw new ArgumentOutOfRangeException(nameof(container), container, "Unknown container."),
        };
    }

    public static VideoContainer ToEntity(this VideoContainerDto container)
    {
        return container switch
        {
            VideoContainerDto.Mp4 => VideoContainer.Mp4,
            VideoContainerDto.Mov => VideoContainer.Mov,
            VideoContainerDto.Webm => VideoContainer.Webm,
            VideoContainerDto.Matroska => VideoContainer.Matroska,
            _ => throw new ArgumentOutOfRangeException(nameof(container), container, "Unknown container."),
        };
    }

    public static VideoCodecDto ToDto(this VideoCodec codec)
    {
        return codec switch
        {
            VideoCodec.H264 => VideoCodecDto.H264,
            VideoCodec.Hevc => VideoCodecDto.Hevc,
            VideoCodec.Vp8 => VideoCodecDto.Vp8,
            VideoCodec.Vp9 => VideoCodecDto.Vp9,
            VideoCodec.Av1 => VideoCodecDto.Av1,
            _ => throw new ArgumentOutOfRangeException(nameof(codec), codec, "Unknown video codec."),
        };
    }

    public static VideoCodec ToEntity(this VideoCodecDto codec)
    {
        return codec switch
        {
            VideoCodecDto.H264 => VideoCodec.H264,
            VideoCodecDto.Hevc => VideoCodec.Hevc,
            VideoCodecDto.Vp8 => VideoCodec.Vp8,
            VideoCodecDto.Vp9 => VideoCodec.Vp9,
            VideoCodecDto.Av1 => VideoCodec.Av1,
            _ => throw new ArgumentOutOfRangeException(nameof(codec), codec, "Unknown video codec."),
        };
    }

    public static AudioCodecDto ToDto(this AudioCodec codec)
    {
        return codec switch
        {
            AudioCodec.Aac => AudioCodecDto.Aac,
            AudioCodec.Mp3 => AudioCodecDto.Mp3,
            AudioCodec.Opus => AudioCodecDto.Opus,
            AudioCodec.Vorbis => AudioCodecDto.Vorbis,
            AudioCodec.Flac => AudioCodecDto.Flac,
            AudioCodec.Ac3 => AudioCodecDto.Ac3,
            AudioCodec.Eac3 => AudioCodecDto.Eac3,
            _ => throw new ArgumentOutOfRangeException(nameof(codec), codec, "Unknown audio codec."),
        };
    }

    public static AudioCodec ToEntity(this AudioCodecDto codec)
    {
        return codec switch
        {
            AudioCodecDto.Aac => AudioCodec.Aac,
            AudioCodecDto.Mp3 => AudioCodec.Mp3,
            AudioCodecDto.Opus => AudioCodec.Opus,
            AudioCodecDto.Vorbis => AudioCodec.Vorbis,
            AudioCodecDto.Flac => AudioCodec.Flac,
            AudioCodecDto.Ac3 => AudioCodec.Ac3,
            AudioCodecDto.Eac3 => AudioCodec.Eac3,
            _ => throw new ArgumentOutOfRangeException(nameof(codec), codec, "Unknown audio codec."),
        };
    }

    public static ReplayGainDto ToDto(this ReplayGainValues values)
    {
        return new ReplayGainDto
        {
            TrackGain = values.TrackGain,
            TrackPeak = values.TrackPeak,
            AlbumGain = values.AlbumGain,
            AlbumPeak = values.AlbumPeak,
        };
    }

    public static PlaybackMethodDto ToDto(this PlaybackMethod method)
    {
        return method switch
        {
            PlaybackMethod.DirectPlay => PlaybackMethodDto.DirectPlay,
            PlaybackMethod.Transcode => PlaybackMethodDto.Transcode,
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, "Unknown playback method."),
        };
    }

    public static TranscodeReasonDto ToDto(this TranscodeReason reason)
    {
        return reason switch
        {
            TranscodeReason.FormatUnsupported => TranscodeReasonDto.FormatUnsupported,
            TranscodeReason.FormatUnknown => TranscodeReasonDto.FormatUnknown,
            TranscodeReason.ContainerUnsupported => TranscodeReasonDto.ContainerUnsupported,
            TranscodeReason.VideoCodecUnsupported => TranscodeReasonDto.VideoCodecUnsupported,
            TranscodeReason.AudioCodecUnsupported => TranscodeReasonDto.AudioCodecUnsupported,
            TranscodeReason.AudioFromVideo => TranscodeReasonDto.AudioFromVideo,
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown transcode reason."),
        };
    }

    public static AudioFormatDto ToDto(this AudioFormat format)
    {
        return format switch
        {
            AudioFormat.Mp3 => AudioFormatDto.Mp3,
            AudioFormat.Flac => AudioFormatDto.Flac,
            AudioFormat.OggFlac => AudioFormatDto.OggFlac,
            AudioFormat.AdtsAac => AudioFormatDto.AdtsAac,
            AudioFormat.Mp4Aac => AudioFormatDto.Mp4Aac,
            AudioFormat.Mp4Alac => AudioFormatDto.Mp4Alac,
            AudioFormat.OggOpus => AudioFormatDto.OggOpus,
            AudioFormat.OggVorbis => AudioFormatDto.OggVorbis,
            AudioFormat.WebmOpus => AudioFormatDto.WebmOpus,
            AudioFormat.WebmVorbis => AudioFormatDto.WebmVorbis,
            AudioFormat.WavPcm => AudioFormatDto.WavPcm,
            AudioFormat.AiffPcm => AudioFormatDto.AiffPcm,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown audio format."),
        };
    }

    public static AudioFormat ToEntity(this AudioFormatDto format)
    {
        return format switch
        {
            AudioFormatDto.Mp3 => AudioFormat.Mp3,
            AudioFormatDto.Flac => AudioFormat.Flac,
            AudioFormatDto.OggFlac => AudioFormat.OggFlac,
            AudioFormatDto.AdtsAac => AudioFormat.AdtsAac,
            AudioFormatDto.Mp4Aac => AudioFormat.Mp4Aac,
            AudioFormatDto.Mp4Alac => AudioFormat.Mp4Alac,
            AudioFormatDto.OggOpus => AudioFormat.OggOpus,
            AudioFormatDto.OggVorbis => AudioFormat.OggVorbis,
            AudioFormatDto.WebmOpus => AudioFormat.WebmOpus,
            AudioFormatDto.WebmVorbis => AudioFormat.WebmVorbis,
            AudioFormatDto.WavPcm => AudioFormat.WavPcm,
            AudioFormatDto.AiffPcm => AudioFormat.AiffPcm,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown audio format."),
        };
    }

    private static string? ContentTypeOf(PlaybackMethod method, string fileContentType, MediaUrl? stream)
    {
        if (stream is null)
        {
            return null;
        }

        return method == PlaybackMethod.DirectPlay ? fileContentType : VideoContentTypes.HlsPlaylist;
    }
}
