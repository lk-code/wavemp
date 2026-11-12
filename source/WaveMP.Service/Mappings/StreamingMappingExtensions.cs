using WaveMP.Core.Streaming.Decisions;
using WaveMP.Core.Streaming.Formats;
using WaveMP.Core.Streaming.Sources;
using WaveMP.Service.Dtos;
using WaveMP.Service.Signatures;

namespace WaveMP.Service.Mappings;

public static class StreamingMappingExtensions
{
    /// <summary>
    /// The playback of a found track; <paramref name="stream"/> is the signed URL of its stream, <c>null</c> if the
    /// track has to be transcoded.
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
            Format = decision.Format?.ToDto(),
            TranscodeReason = decision.Reason?.ToDto(),
            ContentType = stream is null ? null : source.ContentType,
            StreamUrl = stream?.Url,
            ExpiresAt = stream?.ExpiresAt,
            DurationMilliseconds = source.Duration is { } duration
                ? (long)Math.Round(duration.TotalMilliseconds)
                : null,
            ReplayGain = source.ReplayGain.ToDto(),
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
}
