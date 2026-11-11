using AwesomeAssertions;
using WaveMP.Core.Streaming.Decisions;
using WaveMP.Core.Streaming.Formats;

namespace WaveMP.Core.Streaming.Tests.Decisions;

[TestFixture]
public class PlaybackDeciderTests
{
    [Test]
    public void Decide_SupportedFormat_PlaysDirectly()
    {
        PlaybackDecision decision = PlaybackDecider.Decide(
            AudioFormat.Flac,
            new HashSet<AudioFormat> { AudioFormat.Mp3, AudioFormat.Flac });

        decision.Should().Be(new PlaybackDecision(PlaybackMethod.DirectPlay, AudioFormat.Flac, null));
    }

    [Test]
    public void Decide_UnsupportedFormat_NeedsTranscoding()
    {
        PlaybackDecision decision = PlaybackDecider.Decide(
            AudioFormat.Flac,
            new HashSet<AudioFormat> { AudioFormat.Mp3 });

        decision.Should().Be(new PlaybackDecision(
            PlaybackMethod.Transcode,
            AudioFormat.Flac,
            TranscodeReason.FormatUnsupported));
    }

    [Test]
    public void Decide_UnknownFormat_NeedsTranscodingWhateverTheClientSupports()
    {
        PlaybackDecision decision = PlaybackDecider.Decide(null, new HashSet<AudioFormat>(Enum.GetValues<AudioFormat>()));

        decision.Should().Be(new PlaybackDecision(PlaybackMethod.Transcode, null, TranscodeReason.FormatUnknown));
    }

    [Test]
    public void Decide_ClientWithoutFormats_NeedsTranscoding()
    {
        PlaybackDecision decision = PlaybackDecider.Decide(AudioFormat.Mp3, new HashSet<AudioFormat>());

        decision.Method.Should().Be(PlaybackMethod.Transcode);
        decision.Reason.Should().Be(TranscodeReason.FormatUnsupported);
    }
}
