using AwesomeAssertions;
using NSubstitute;
using WaveMP.Core.Streaming.Offsets;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Streaming.Tests.Offsets;

[TestFixture]
public class VideoOffsetServiceTests
{
    private readonly Guid _trackId = Guid.NewGuid();

    private ITrackRepository _tracks = null!;
    private VideoOffsetService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _tracks = Substitute.For<ITrackRepository>();
        _tracks.FindPrimaryFileAsync(Arg.Any<Guid>(), Arg.Any<MediaFileKind>(), Arg.Any<CancellationToken>())
            .Returns((TrackFileRow?)null);
        _tracks.SetVideoOffsetAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _service = new VideoOffsetService(_tracks);
    }

    [Test]
    public async Task GetAsync_TrackWithVideo_ReturnsItsOffset()
    {
        GivenTrack(withVideo: true, offset: -750);

        VideoOffsetResult result = await _service.GetAsync(_trackId);

        result.Should().Be(new VideoOffsetResult(VideoOffsetStatus.Found, _trackId, -750));
        await _tracks.Received(1).FindPrimaryFileAsync(_trackId, MediaFileKind.Video, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetAsync_UnknownTrack_ReturnsTrackNotFound()
    {
        VideoOffsetResult result = await _service.GetAsync(_trackId);

        result.Status.Should().Be(VideoOffsetStatus.TrackNotFound);
        result.IsFound.Should().BeFalse();
    }

    [Test]
    public async Task GetAsync_TrackWithoutVideo_ReturnsNoVideo()
    {
        GivenTrack(withVideo: false, offset: 0);

        VideoOffsetResult result = await _service.GetAsync(_trackId);

        result.Status.Should().Be(VideoOffsetStatus.NoVideo);
    }

    [TestCase(VideoOffsets.MinMilliseconds)]
    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(1500)]
    [TestCase(VideoOffsets.MaxMilliseconds)]
    public async Task SetAsync_ValidOffset_StoresIt(int milliseconds)
    {
        GivenTrack(withVideo: true, offset: 200);

        VideoOffsetResult result = await _service.SetAsync(_trackId, milliseconds);

        result.Should().Be(new VideoOffsetResult(VideoOffsetStatus.Found, _trackId, milliseconds));
        await _tracks.Received(1).SetVideoOffsetAsync(_trackId, milliseconds, Arg.Any<CancellationToken>());
    }

    [TestCase(VideoOffsets.MinMilliseconds - 1)]
    [TestCase(VideoOffsets.MaxMilliseconds + 1)]
    [TestCase(int.MinValue)]
    [TestCase(int.MaxValue)]
    public async Task SetAsync_OffsetOutOfRange_ReturnsInvalidWithoutStoringIt(int milliseconds)
    {
        GivenTrack(withVideo: true, offset: 0);

        VideoOffsetResult result = await _service.SetAsync(_trackId, milliseconds);

        result.Status.Should().Be(VideoOffsetStatus.Invalid);
        await _tracks.DidNotReceiveWithAnyArgs().SetVideoOffsetAsync(default, default, default);
    }

    [Test]
    public async Task SetAsync_UnknownTrack_ReturnsTrackNotFoundWithoutStoringIt()
    {
        VideoOffsetResult result = await _service.SetAsync(_trackId, 100);

        result.Status.Should().Be(VideoOffsetStatus.TrackNotFound);
        await _tracks.DidNotReceiveWithAnyArgs().SetVideoOffsetAsync(default, default, default);
    }

    [Test]
    public async Task SetAsync_TrackWithoutVideo_ReturnsNoVideoWithoutStoringIt()
    {
        GivenTrack(withVideo: false, offset: 0);

        VideoOffsetResult result = await _service.SetAsync(_trackId, 100);

        result.Status.Should().Be(VideoOffsetStatus.NoVideo);
        await _tracks.DidNotReceiveWithAnyArgs().SetVideoOffsetAsync(default, default, default);
    }

    [Test]
    public async Task SetAsync_TrackRemovedInTheMeantime_ReturnsTrackNotFound()
    {
        GivenTrack(withVideo: true, offset: 0);
        _tracks.SetVideoOffsetAsync(_trackId, 100, Arg.Any<CancellationToken>()).Returns(false);

        VideoOffsetResult result = await _service.SetAsync(_trackId, 100);

        result.Status.Should().Be(VideoOffsetStatus.TrackNotFound);
    }

    [TestCase(VideoOffsets.MinMilliseconds, true)]
    [TestCase(VideoOffsets.MaxMilliseconds, true)]
    [TestCase(VideoOffsets.MinMilliseconds - 1, false)]
    [TestCase(VideoOffsets.MaxMilliseconds + 1, false)]
    public void IsValid_Always_AcceptsTheLimits(int milliseconds, bool expected)
    {
        VideoOffsets.IsValid(milliseconds).Should().Be(expected);
    }

    private void GivenTrack(bool withVideo, int offset)
    {
        MediaFile? video = withVideo
            ? new MediaFile
            {
                Id = Guid.NewGuid(),
                RelativePath = "Song.mkv",
                DirectoryPath = string.Empty,
                FileName = "Song.mkv",
                BaseName = "Song",
                NormalizedBaseName = "SONG",
                Extension = "mkv",
                Kind = MediaFileKind.Video,
                Size = 10,
                ModifiedAt = DateTimeOffset.UnixEpoch,
                IsPrimary = true,
                IsLossless = false,
                HasEmbeddedPicture = false,
                IsWritable = false,
                MetadataVersion = 2,
                FirstSeenAt = DateTimeOffset.UnixEpoch,
                ScannedAt = DateTimeOffset.UnixEpoch,
            }
            : null;
        _tracks.FindPrimaryFileAsync(_trackId, MediaFileKind.Video, Arg.Any<CancellationToken>())
            .Returns(new TrackFileRow(_trackId, video, offset));
    }
}
