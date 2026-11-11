using AwesomeAssertions;
using WaveMP.Core.Streaming.Decisions;
using WaveMP.Core.Streaming.Formats;
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
    public void ToDto_Transcode_HasNoStream()
    {
        TrackPlaybackDto dto = _source.ToDto(
            new PlaybackDecision(PlaybackMethod.Transcode, null, TranscodeReason.FormatUnknown),
            null);

        dto.Method.Should().Be(PlaybackMethodDto.Transcode);
        dto.Format.Should().BeNull();
        dto.TranscodeReason.Should().Be(TranscodeReasonDto.FormatUnknown);
        dto.ContentType.Should().BeNull();
        dto.StreamUrl.Should().BeNull();
        dto.ExpiresAt.Should().BeNull();
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
}
