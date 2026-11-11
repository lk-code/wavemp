using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WaveMP.Core.Caching.Covers;
using WaveMP.Core.Library.Covers;
using WaveMP.Core.Playlists.Covers;
using WaveMP.Core.Playlists.Playlists;
using WaveMP.Data.Projections;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Playlists.Tests.Covers;

[TestFixture]
public class PlaylistCoverServiceTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 12, 20, 0, 0, TimeSpan.Zero);

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _playlistId = Guid.NewGuid();

    private IPlaylistRepository _playlists = null!;
    private IPlaylistService _service = null!;
    private IPlaylistCoverStore _store = null!;
    private PlaylistCoverService _covers = null!;

    [SetUp]
    public void SetUp()
    {
        _playlists = Substitute.For<IPlaylistRepository>();
        _service = Substitute.For<IPlaylistService>();
        _store = Substitute.For<IPlaylistCoverStore>();
        TimeProvider timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(_now);
        _covers = new PlaylistCoverService(
            _playlists,
            _service,
            _store,
            timeProvider,
            NullLogger<PlaylistCoverService>.Instance);
    }

    [Test]
    public async Task SetAsync_Saved_RecordsTheUploadAndReturnsThePlaylist()
    {
        GivenPlaylist();
        _store.SaveAsync(_playlistId, Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns(CoverSaveStatus.Saved);
        _playlists.SetCoverAsync(_userId, _playlistId, _now, _now, Arg.Any<CancellationToken>()).Returns(true);
        PlaylistItem item = new(Row(_now), new PlaylistCover(PlaylistCoverKind.Custom, "v", []));
        _service.GetAsync(_userId, _playlistId, Arg.Any<CancellationToken>()).Returns(item);

        PlaylistCoverResult result = await _covers.SetAsync(_userId, _playlistId, Stream.Null);

        result.Should().Be(new PlaylistCoverResult(PlaylistCoverStatus.Saved, item));
    }

    [TestCase(CoverSaveStatus.UnsupportedFormat, PlaylistCoverStatus.UnsupportedFormat)]
    [TestCase(CoverSaveStatus.Unreadable, PlaylistCoverStatus.Unreadable)]
    [TestCase(CoverSaveStatus.Unavailable, PlaylistCoverStatus.Unavailable)]
    public async Task SetAsync_NotSaved_ReturnsWhyAndKeepsThePlaylist(CoverSaveStatus saved, PlaylistCoverStatus expected)
    {
        GivenPlaylist();
        _store.SaveAsync(_playlistId, Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns(saved);

        PlaylistCoverResult result = await _covers.SetAsync(_userId, _playlistId, Stream.Null);

        result.Status.Should().Be(expected);
        await _playlists.DidNotReceiveWithAnyArgs().SetCoverAsync(default, default, default, default);
    }

    [Test]
    public async Task SetAsync_MissingPlaylist_StoresNothing()
    {
        PlaylistCoverResult result = await _covers.SetAsync(_userId, _playlistId, Stream.Null);

        result.Status.Should().Be(PlaylistCoverStatus.NotFound);
        await _store.DidNotReceiveWithAnyArgs().SaveAsync(default, default!);
    }

    [Test]
    public async Task SetAsync_PlaylistDeletedDuringTheUpload_RemovesTheFiles()
    {
        GivenPlaylist();
        _store.SaveAsync(_playlistId, Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns(CoverSaveStatus.Saved);

        PlaylistCoverResult result = await _covers.SetAsync(_userId, _playlistId, Stream.Null);

        result.Status.Should().Be(PlaylistCoverStatus.NotFound);
        _store.Received(1).Delete(_playlistId);
    }

    [Test]
    public async Task RemoveAsync_OwnCover_ForgetsItAndRemovesTheFiles()
    {
        GivenPlaylist(_now);
        _playlists.SetCoverAsync(_userId, _playlistId, null, _now, Arg.Any<CancellationToken>()).Returns(true);

        bool removed = await _covers.RemoveAsync(_userId, _playlistId);

        removed.Should().BeTrue();
        _store.Received(1).Delete(_playlistId);
    }

    [Test]
    public async Task RemoveAsync_MissingPlaylist_ReturnsFalse()
    {
        (await _covers.RemoveAsync(_userId, _playlistId)).Should().BeFalse();
    }

    [Test]
    public async Task GetAsync_OwnCover_DeliversTheWebPWithItsVersionAndSize()
    {
        GivenPlaylist(_now);
        MemoryStream content = new([1, 2, 3]);
        _store.Open(_playlistId, 300).Returns(content);

        CoverDeliveryResult result = await _covers.GetAsync(_userId, _playlistId, 120);

        result.Status.Should().Be(CoverStatus.Found);
        result.Delivery!.Content.Should().BeSameAs(content);
        result.Delivery.ContentType.Should().Be("image/webp");
        result.Delivery.ETag.Should().Be($"{PlaylistCoverVersions.Of(_now)}-300");
        result.Delivery.LastModified.Should().Be(_now);
    }

    [Test]
    public async Task GetAsync_WithoutSize_DeliversTheLargestPicture()
    {
        GivenPlaylist(_now);
        _store.Open(_playlistId, 1000).Returns(new MemoryStream());

        CoverDeliveryResult result = await _covers.GetAsync(_userId, _playlistId, null);

        result.Status.Should().Be(CoverStatus.Found);
    }

    [Test]
    public async Task GetAsync_WithoutOwnCover_ReturnsNotFound()
    {
        GivenPlaylist();

        (await _covers.GetAsync(_userId, _playlistId, 96)).Status.Should().Be(CoverStatus.NotFound);
    }

    [Test]
    public async Task GetAsync_MissingPlaylist_ReturnsOwnerNotFound()
    {
        (await _covers.GetAsync(_userId, _playlistId, 96)).Status.Should().Be(CoverStatus.OwnerNotFound);
    }

    [Test]
    public async Task RemoveOrphansAsync_Always_RemovesTheCoversOfMissingPlaylists()
    {
        var orphan = Guid.NewGuid();
        _store.ListPlaylistIds().Returns([_playlistId, orphan]);
        _playlists.ListExistingIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<Guid> { _playlistId });

        await _covers.RemoveOrphansAsync();

        _store.Received(1).Delete(orphan);
        _store.DidNotReceive().Delete(_playlistId);
    }

    private void GivenPlaylist(DateTimeOffset? coverUpdatedAt = null)
    {
        _playlists.FindAsync(_userId, _playlistId, Arg.Any<CancellationToken>()).Returns(Row(coverUpdatedAt));
    }

    private PlaylistRow Row(DateTimeOffset? coverUpdatedAt)
    {
        return new PlaylistRow(_playlistId, "Mix", 0, 0, coverUpdatedAt, _now, _now);
    }
}
