using AwesomeAssertions;
using NSubstitute;
using WaveMP.Core.Caching.Stores;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Snapshots;
using WaveMP.Core.Streaming.Formats;
using WaveMP.Core.Streaming.Sources;
using WaveMP.Core.Transcoding.Accelerators;
using WaveMP.Core.Transcoding.Options;
using WaveMP.Core.Transcoding.Profiles;
using WaveMP.Core.Transcoding.Services;
using WaveMP.Core.Transcoding.Transcodes;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Transcoding.Tests.Services;

[TestFixture]
public class TranscodingServiceTests
{
    private static readonly Guid _trackId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid _fileId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset _modified = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private IStreamService _streams = null!;
    private ITranscodeManager _manager = null!;
    private IHardwareAccelerationService _hardware = null!;
    private ICacheStore _cache = null!;
    private ITrackRepository _tracks = null!;
    private ISettingsSnapshot<TranscodingSettings> _settings = null!;
    private TranscodingService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _streams = Substitute.For<IStreamService>();
        _manager = Substitute.For<ITranscodeManager>();
        _hardware = Substitute.For<IHardwareAccelerationService>();
        _hardware.Current.Returns(HardwareAccelerationStatus.Detecting);
        _cache = Substitute.For<ICacheStore>();
        _cache.IsAvailable.Returns(true);
        _tracks = Substitute.For<ITrackRepository>();
        _settings = Substitute.For<ISettingsSnapshot<TranscodingSettings>>();
        _settings.Current.Returns(new TranscodingSettings { MaxParallelJobs = 3 });
        _service = new TranscodingService(
            _streams,
            _manager,
            _hardware,
            _cache,
            _tracks,
            _settings,
            Microsoft.Extensions.Options.Options.Create(new TranscodingOptions
            {
                SegmentDuration = TimeSpan.FromSeconds(6),
            }));
    }

    [Test]
    public async Task ResolveAsync_Video_BuildsTheVariantOfTheVideo()
    {
        _streams.GetVideoSourceAsync(_trackId, Arg.Any<CancellationToken>())
            .Returns(VideoSourceResult.Found(Video(TimeSpan.FromSeconds(61))));

        TranscodeVariantResult result = await _service.ResolveAsync(_trackId, HlsMedia.Video);

        result.IsFound.Should().BeTrue();
        result.Variant!.Input.Should().Be(new TranscodeInput(
            _trackId,
            _fileId,
            "/media/Song.webm",
            1000,
            _modified,
            TimeSpan.FromSeconds(61),
            true,
            3840,
            2160));
        result.Variant.Profile.Size.Should().Be(new OutputSize(1920, 1080));
        result.Variant.Profile.SegmentDuration.Should().Be(TimeSpan.FromSeconds(6));
        result.Variant.SegmentCount.Should().Be(11);
    }

    [Test]
    public async Task ResolveAsync_Audio_UsesTheSoundSource()
    {
        _streams.GetSoundSourceAsync(_trackId, Arg.Any<CancellationToken>())
            .Returns(StreamSourceResult.Found(Sound(TimeSpan.FromSeconds(30))));

        TranscodeVariantResult result = await _service.ResolveAsync(_trackId, HlsMedia.Audio);

        result.Variant!.Media.Should().Be(HlsMedia.Audio);
        result.Variant.Input.FullPath.Should().Be("/media/Song.wma");
        result.Variant.Profile.AudioBitrateKbps.Should().Be(256);
    }

    [TestCase(StreamSourceStatus.TrackNotFound, TranscodeVariantStatus.TrackNotFound)]
    [TestCase(StreamSourceStatus.NoVideo, TranscodeVariantStatus.NoMedia)]
    [TestCase(StreamSourceStatus.FileMissing, TranscodeVariantStatus.FileMissing)]
    public async Task ResolveAsync_NoVideo_TellsWhy(StreamSourceStatus status, TranscodeVariantStatus expected)
    {
        _streams.GetVideoSourceAsync(_trackId, Arg.Any<CancellationToken>()).Returns(VideoSourceResult.Of(status));

        TranscodeVariantResult result = await _service.ResolveAsync(_trackId, HlsMedia.Video);

        result.Should().Be(TranscodeVariantResult.Of(expected));
    }

    [Test]
    public async Task ResolveAsync_UnknownDuration_IsDurationUnknown()
    {
        _streams.GetVideoSourceAsync(_trackId, Arg.Any<CancellationToken>())
            .Returns(VideoSourceResult.Found(Video(null)));

        TranscodeVariantResult result = await _service.ResolveAsync(_trackId, HlsMedia.Video);

        result.Status.Should().Be(TranscodeVariantStatus.DurationUnknown);
    }

    [Test]
    public async Task GetSegmentAsync_CurrentVariant_AsksTheManager()
    {
        _streams.GetVideoSourceAsync(_trackId, Arg.Any<CancellationToken>())
            .Returns(VideoSourceResult.Found(Video(TimeSpan.FromSeconds(61))));
        TranscodeVariant variant = (await _service.ResolveAsync(_trackId, HlsMedia.Video)).Variant!;
        var userId = Guid.NewGuid();
        var ready = SegmentResult.Ready(new MemoryStream([1]));
        _manager.GetSegmentAsync(variant, 3, userId, Arg.Any<CancellationToken>()).Returns(ready);

        SegmentResult result = await _service.GetSegmentAsync(_trackId, HlsMedia.Video, variant.Id, 3, userId);

        result.Should().BeSameAs(ready);
    }

    [Test]
    public async Task GetSegmentAsync_OtherVariant_IsOutdated()
    {
        _streams.GetVideoSourceAsync(_trackId, Arg.Any<CancellationToken>())
            .Returns(VideoSourceResult.Found(Video(TimeSpan.FromSeconds(61))));

        SegmentResult result = await _service.GetSegmentAsync(
            _trackId,
            HlsMedia.Video,
            "00000000000000000000000000000000",
            0,
            null);

        result.Status.Should().Be(SegmentStatus.VariantOutdated);
        await _manager.DidNotReceiveWithAnyArgs().GetSegmentAsync(null!, 0, null);
    }

    [Test]
    public async Task GetSegmentAsync_TrackMissing_IsTrackNotFound()
    {
        _streams.GetSoundSourceAsync(_trackId, Arg.Any<CancellationToken>())
            .Returns(StreamSourceResult.Of(StreamSourceStatus.TrackNotFound));

        SegmentResult result = await _service.GetSegmentAsync(_trackId, HlsMedia.Audio, "x", 0, null);

        result.Status.Should().Be(SegmentStatus.TrackNotFound);
    }

    [Test]
    public async Task GetSegmentAsync_FfmpegMissing_IsUnavailable()
    {
        _streams.GetVideoSourceAsync(_trackId, Arg.Any<CancellationToken>())
            .Returns(VideoSourceResult.Found(Video(TimeSpan.FromSeconds(61))));
        TranscodeVariant variant = (await _service.ResolveAsync(_trackId, HlsMedia.Video)).Variant!;
        _hardware.Current.Returns(HardwareAccelerationStatus.Detecting with { FfmpegAvailable = false });

        SegmentResult result = await _service.GetSegmentAsync(_trackId, HlsMedia.Video, variant.Id, 0, null);

        result.Status.Should().Be(SegmentStatus.Unavailable);
        _service.IsAvailable.Should().BeFalse();
    }

    [Test]
    public void GetStatus_Always_CombinesHardwareCacheAndJobs()
    {
        _manager.ListJobs().Returns([Job(DateTimeOffset.UnixEpoch)]);
        _cache.IsAvailable.Returns(false);

        TranscodingStatus status = _service.GetStatus();

        status.Should().Be(new TranscodingStatus(HardwareAccelerationStatus.Detecting, false, 1, 3));
        status.Available.Should().BeFalse();
    }

    [Test]
    public async Task ListJobsAsync_Page_AddsTheTracks()
    {
        var userId = Guid.NewGuid();
        TranscodeJobInfo first = Job(DateTimeOffset.UnixEpoch.AddMinutes(2));
        TranscodeJobInfo second = Job(DateTimeOffset.UnixEpoch.AddMinutes(1), Guid.NewGuid());
        _manager.ListJobs().Returns([first, second]);
        TrackRow track = Track(_trackId);
        _tracks.ListByIdsAsync(
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { _trackId })),
                userId,
                Arg.Any<CancellationToken>())
            .Returns([track]);

        Page<TranscodeJobItem> page = await _service.ListJobsAsync(0, 1, userId);

        page.Should().BeEquivalentTo(new Page<TranscodeJobItem>([new TranscodeJobItem(first, track)], 0, 1, 2));
    }

    [Test]
    public async Task ListJobsAsync_RemovedTrack_HasNoTrack()
    {
        TranscodeJobInfo job = Job(DateTimeOffset.UnixEpoch);
        _manager.ListJobs().Returns([job]);
        _tracks.ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), null, Arg.Any<CancellationToken>())
            .Returns([]);

        Page<TranscodeJobItem> page = await _service.ListJobsAsync(0, 10, null);

        page.Items.Should().ContainSingle().Which.Track.Should().BeNull();
    }

    [Test]
    public void CancelJob_Id_AsksTheManager()
    {
        var id = Guid.NewGuid();
        _manager.Cancel(id).Returns(true);

        _service.CancelJob(id).Should().BeTrue();
        _service.CancelJob(Guid.NewGuid()).Should().BeFalse();
    }

    private static VideoSource Video(TimeSpan? duration)
    {
        return new VideoSource(
            _trackId,
            _fileId,
            "/media/Song.webm",
            VideoContainer.Webm,
            VideoCodec.Vp9,
            AudioCodec.Opus,
            true,
            "yuv420p",
            3840,
            2160,
            48000,
            VideoContentTypes.Webm,
            1000,
            _modified,
            "etag",
            duration,
            true);
    }

    private static StreamSource Sound(TimeSpan? duration)
    {
        return new StreamSource(
            _trackId,
            _fileId,
            "/media/Song.wma",
            null,
            "audio/x-ms-wma",
            1000,
            _modified,
            "etag",
            duration,
            new ReplayGainValues(null, null, null, null),
            MediaFileKind.Audio);
    }

    private static TranscodeJobInfo Job(DateTimeOffset startedAt, Guid? trackId = null)
    {
        return new TranscodeJobInfo(
            Guid.NewGuid(),
            trackId ?? _trackId,
            HlsMedia.Video,
            HardwareAccelerator.None,
            0,
            1,
            10,
            startedAt,
            startedAt,
            null);
    }

    private static TrackRow Track(Guid id)
    {
        return new TrackRow(
            id,
            "Song",
            null,
            "Artist",
            null,
            null,
            null,
            null,
            false,
            null,
            null,
            null,
            null,
            TimeSpan.FromSeconds(61),
            true,
            true,
            false,
            DateTimeOffset.UnixEpoch,
            false,
            0);
    }
}
