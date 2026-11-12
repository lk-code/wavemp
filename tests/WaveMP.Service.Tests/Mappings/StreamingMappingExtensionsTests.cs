using AwesomeAssertions;
using WaveMP.Core.Scanning.Writers;
using WaveMP.Core.Streaming.Decisions;
using WaveMP.Core.Streaming.Formats;
using WaveMP.Core.Streaming.Offsets;
using WaveMP.Core.Streaming.Sources;
using WaveMP.Service.Dtos;
using WaveMP.Service.Mappings;
using WaveMP.Service.Signatures;

namespace WaveMP.Service.Tests.Mappings;

[TestFixture]
public class StreamingMappingExtensionsTests
{
    private static readonly StreamSource _source = new(
        Guid.Parse("0195f3a0-0000-7000-8000-000000000001"),
        Guid.Parse("0195f3a0-0000-7000-8000-000000000002"),
        "/media/song.flac",
        AudioFormat.Flac,
        "audio/flac",
        1234,
        DateTimeOffset.UnixEpoch,
        "tag",
        TimeSpan.FromMilliseconds(2500.4),
        new ReplayGainValues(-6.5, 0.98, null, null));

    [Test]
    public void ToDto_DirectPlay_CarriesTheStream()
    {
        MediaUrl stream = new("/api/v1/tracks/x/stream?sig=a", DateTimeOffset.UnixEpoch.AddHours(1));

        TrackPlaybackDto dto = _source.ToDto(
            new PlaybackDecision(PlaybackMethod.DirectPlay, AudioFormat.Flac, null),
            stream);

        dto.Should().BeEquivalentTo(new TrackPlaybackDto
        {
            TrackId = _source.TrackId,
            Method = PlaybackMethodDto.DirectPlay,
            Protocol = StreamProtocolDto.File,
            Format = AudioFormatDto.Flac,
            TranscodeReason = null,
            ContentType = "audio/flac",
            StreamUrl = stream.Url,
            ExpiresAt = stream.ExpiresAt,
            DurationMilliseconds = 2500,
            ReplayGain = new ReplayGainDto { TrackGain = -6.5, TrackPeak = 0.98 },
        });
    }

    [Test]
    public void ToDto_TranscodeWithoutTranscoding_HasNoStream()
    {
        TrackPlaybackDto dto = _source.ToDto(
            new PlaybackDecision(PlaybackMethod.Transcode, null, TranscodeReason.FormatUnknown),
            null);

        dto.Method.Should().Be(PlaybackMethodDto.Transcode);
        dto.Protocol.Should().Be(StreamProtocolDto.Hls);
        dto.Format.Should().BeNull();
        dto.TranscodeReason.Should().Be(TranscodeReasonDto.FormatUnknown);
        dto.ContentType.Should().BeNull();
        dto.StreamUrl.Should().BeNull();
        dto.ExpiresAt.Should().BeNull();
    }

    [Test]
    public void ToDto_TranscodeWithHls_CarriesThePlaylist()
    {
        MediaUrl stream = new("/api/v1/tracks/x/hls/master.m3u8?media=Audio&sig=a", DateTimeOffset.UnixEpoch);

        TrackPlaybackDto dto = _source.ToDto(
            new PlaybackDecision(PlaybackMethod.Transcode, AudioFormat.Flac, TranscodeReason.FormatUnsupported),
            stream);

        dto.Protocol.Should().Be(StreamProtocolDto.Hls);
        dto.ContentType.Should().Be("application/vnd.apple.mpegurl");
        dto.StreamUrl.Should().Be(stream.Url);
        dto.ExpiresAt.Should().Be(stream.ExpiresAt);
    }

    [Test]
    public void ToDto_VideoDirectPlay_DescribesTheFile()
    {
        VideoSource source = Video();
        MediaUrl stream = new("/api/v1/tracks/x/video?sig=a", DateTimeOffset.UnixEpoch.AddHours(1));

        TrackVideoPlaybackDto dto = source.ToDto(VideoPlaybackDecision.DirectPlay, stream);

        dto.Should().BeEquivalentTo(new TrackVideoPlaybackDto
        {
            TrackId = source.TrackId,
            Method = PlaybackMethodDto.DirectPlay,
            Protocol = StreamProtocolDto.File,
            TranscodeReason = null,
            Container = VideoContainerDto.Mp4,
            VideoCodec = VideoCodecDto.H264,
            AudioCodec = AudioCodecDto.Aac,
            HasAudio = true,
            Width = 1920,
            Height = 1080,
            ContentType = "video/mp4",
            StreamUrl = stream.Url,
            ExpiresAt = stream.ExpiresAt,
            DurationMilliseconds = 61000,
            OffsetMilliseconds = -1500,
        });
    }

    [Test]
    public void ToDto_VideoOffset_CarriesTrackAndOffset()
    {
        var trackId = Guid.NewGuid();

        TrackVideoOffsetDto dto = new VideoOffsetResult(VideoOffsetStatus.Found, trackId, 2500).ToDto();

        dto.TrackId.Should().Be(trackId);
        dto.OffsetMilliseconds.Should().Be(2500);
    }

    [Test]
    public void ToDto_VideoTranscode_CarriesThePlaylistOrNothing()
    {
        VideoSource source = Video() with { Container = null };
        var decision = VideoPlaybackDecision.Transcode(TranscodeReason.ContainerUnsupported);

        TrackVideoPlaybackDto withHls = source.ToDto(
            decision,
            new MediaUrl("/api/v1/tracks/x/hls/master.m3u8?sig=a", DateTimeOffset.UnixEpoch));
        TrackVideoPlaybackDto without = source.ToDto(decision, null);

        withHls.Protocol.Should().Be(StreamProtocolDto.Hls);
        withHls.TranscodeReason.Should().Be(TranscodeReasonDto.ContainerUnsupported);
        withHls.Container.Should().BeNull();
        withHls.ContentType.Should().Be("application/vnd.apple.mpegurl");
        without.StreamUrl.Should().BeNull();
        without.ContentType.Should().BeNull();
    }

    [Test]
    public void ToCapabilities_Values_MapToTheFormatsOfTheStreaming()
    {
        VideoCapabilities capabilities = StreamingMappingExtensions.ToCapabilities(
            [VideoContainerDto.Webm],
            [VideoCodecDto.Vp9, VideoCodecDto.Av1],
            null);

        capabilities.Containers.Should().BeEquivalentTo([VideoContainer.Webm]);
        capabilities.VideoCodecs.Should().BeEquivalentTo([VideoCodec.Vp9, VideoCodec.Av1]);
        capabilities.AudioCodecs.Should().BeEmpty();
    }

    [Test]
    public void VideoEnums_MapBothWays()
    {
        foreach (VideoContainer container in Enum.GetValues<VideoContainer>())
        {
            container.ToDto().ToString().Should().Be(container.ToString());
            container.ToDto().ToEntity().Should().Be(container);
        }

        foreach (VideoCodec codec in Enum.GetValues<VideoCodec>())
        {
            codec.ToDto().ToString().Should().Be(codec.ToString());
            codec.ToDto().ToEntity().Should().Be(codec);
        }

        foreach (AudioCodec codec in Enum.GetValues<AudioCodec>())
        {
            codec.ToDto().ToString().Should().Be(codec.ToString());
            codec.ToDto().ToEntity().Should().Be(codec);
        }

        Enum.GetValues<VideoContainerDto>().Should().HaveSameCount(Enum.GetValues<VideoContainer>());
        Enum.GetValues<VideoCodecDto>().Should().HaveSameCount(Enum.GetValues<VideoCodec>());
        Enum.GetValues<AudioCodecDto>().Should().HaveSameCount(Enum.GetValues<AudioCodec>());
    }

    [Test]
    public void AudioFormats_MapBothWays()
    {
        foreach (AudioFormat format in Enum.GetValues<AudioFormat>())
        {
            AudioFormatDto dto = format.ToDto();

            dto.ToString().Should().Be(format.ToString());
            dto.ToEntity().Should().Be(format);
        }

        Enum.GetValues<AudioFormatDto>().Should().HaveSameCount(Enum.GetValues<AudioFormat>());
    }

    [Test]
    public void PlaybackEnums_MapEveryValue()
    {
        Enum.GetValues<PlaybackMethod>().Select(method => method.ToDto().ToString())
            .Should().Equal(Enum.GetValues<PlaybackMethod>().Select(method => method.ToString()));
        Enum.GetValues<TranscodeReason>().Select(reason => reason.ToDto().ToString())
            .Should().Equal(Enum.GetValues<TranscodeReason>().Select(reason => reason.ToString()));
    }

    private static VideoSource Video()
    {
        return new VideoSource(
            Guid.Parse("0195f3a0-0000-7000-8000-000000000003"),
            Guid.Parse("0195f3a0-0000-7000-8000-000000000004"),
            "/media/clip.mp4",
            VideoContainer.Mp4,
            VideoCodec.H264,
            AudioCodec.Aac,
            true,
            "yuv420p",
            1920,
            1080,
            48000,
            "video/mp4",
            5000,
            DateTimeOffset.UnixEpoch,
            "tag",
            TimeSpan.FromSeconds(61),
            true,
            -1500);
    }

    [Test]
    public void VideoMetadataVersion_IsTheVersionOfVideosOfTheScan()
    {
        VideoFormats.MetadataVersion.Should().Be(ScannerVersions.VideoMetadata);
    }
}
