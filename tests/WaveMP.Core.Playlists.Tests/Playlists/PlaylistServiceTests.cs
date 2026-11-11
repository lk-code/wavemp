using AwesomeAssertions;
using NSubstitute;
using WaveMP.Core.Playlists.Covers;
using WaveMP.Core.Playlists.Options;
using WaveMP.Core.Playlists.Playlists;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Playlists.Tests.Playlists;

[TestFixture]
public class PlaylistServiceTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 12, 20, 0, 0, TimeSpan.Zero);

    private readonly Guid _userId = Guid.NewGuid();

    private IPlaylistRepository _playlists = null!;
    private ITrackRepository _tracks = null!;
    private IPlaylistCoverStore _coverStore = null!;
    private PlaylistOptions _options = null!;
    private PlaylistService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _playlists = Substitute.For<IPlaylistRepository>();
        _tracks = Substitute.For<ITrackRepository>();
        _coverStore = Substitute.For<IPlaylistCoverStore>();
        _options = new PlaylistOptions();
        _playlists.ListCoverCandidatesAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
        _tracks.ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns([]);
        TimeProvider timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(_now);
        _service = new PlaylistService(
            _playlists,
            _tracks,
            _coverStore,
            timeProvider,
            Microsoft.Extensions.Options.Options.Create(_options));
    }

    [Test]
    public async Task CreateAsync_ValidName_CreatesThePlaylistOfTheUserWithTheNormalizedName()
    {
        var track = Guid.NewGuid();
        Playlist? created = null;
        _playlists.CreateAsync(Arg.Do<Playlist>(playlist => created = playlist), Arg.Any<IReadOnlyList<Guid>>())
            .Returns(PlaylistEntryChange.Of(PlaylistEntryChangeStatus.Done));
        _playlists.FindAsync(_userId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => Row(call.ArgAt<Guid>(1), created!.Name));

        PlaylistResult result = await _service.CreateAsync(_userId, "  Road   Trip ", [track]);

        result.IsOk.Should().BeTrue();
        created!.UserId.Should().Be(_userId);
        created.Name.Should().Be("Road Trip");
        created.NormalizedName.Should().Be("ROAD TRIP");
        created.CreatedAt.Should().Be(_now);
        result.Item!.Playlist.Name.Should().Be("Road Trip");
        await _playlists.Received(1).CreateAsync(
            Arg.Any<Playlist>(),
            Arg.Is<IReadOnlyList<Guid>>(ids => ids.SequenceEqual(new[] { track })),
            Arg.Any<CancellationToken>());
    }

    [TestCase(null)]
    [TestCase("   ")]
    public async Task CreateAsync_InvalidName_ReturnsInvalidName(string? name)
    {
        PlaylistResult result = await _service.CreateAsync(_userId, name, []);

        result.Status.Should().Be(PlaylistStatus.InvalidName);
        await _playlists.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!);
    }

    [Test]
    public async Task CreateAsync_UnknownTrack_ReturnsTrackNotFound()
    {
        _playlists.CreateAsync(Arg.Any<Playlist>(), Arg.Any<IReadOnlyList<Guid>>())
            .Returns(PlaylistEntryChange.Of(PlaylistEntryChangeStatus.TrackNotFound));

        PlaylistResult result = await _service.CreateAsync(_userId, "Mix", [Guid.NewGuid()]);

        result.Status.Should().Be(PlaylistStatus.TrackNotFound);
    }

    [Test]
    public async Task CreateAsync_MoreTracksThanAllowed_ReturnsFull()
    {
        _options.MaxEntries = 1;

        PlaylistResult result = await _service.CreateAsync(_userId, "Mix", [Guid.NewGuid(), Guid.NewGuid()]);

        result.Status.Should().Be(PlaylistStatus.Full);
    }

    [Test]
    public async Task RenameAsync_MissingPlaylist_ReturnsNotFound()
    {
        PlaylistResult result = await _service.RenameAsync(_userId, Guid.NewGuid(), "New");

        result.Status.Should().Be(PlaylistStatus.NotFound);
    }

    [Test]
    public async Task DeleteAsync_ExistingPlaylist_DeletesItsCover()
    {
        var id = Guid.NewGuid();
        _playlists.DeleteAsync(_userId, id, Arg.Any<CancellationToken>()).Returns(true);

        bool deleted = await _service.DeleteAsync(_userId, id);

        deleted.Should().BeTrue();
        _coverStore.Received(1).Delete(id);
    }

    [Test]
    public async Task DeleteAsync_MissingPlaylist_KeepsTheStore()
    {
        bool deleted = await _service.DeleteAsync(_userId, Guid.NewGuid());

        deleted.Should().BeFalse();
        _coverStore.DidNotReceiveWithAnyArgs().Delete(default);
    }

    [Test]
    public async Task GetAsync_OwnCover_ReturnsItsVersion()
    {
        var id = Guid.NewGuid();
        _playlists.FindAsync(_userId, id, Arg.Any<CancellationToken>()).Returns(Row(id, "Mix", _now));

        PlaylistItem? item = await _service.GetAsync(_userId, id);

        item!.Cover.Kind.Should().Be(PlaylistCoverKind.Custom);
        item.Cover.Version.Should().Be(PlaylistCoverVersions.Of(_now));
        item.Cover.TrackIds.Should().BeEmpty();
        await _playlists.DidNotReceiveWithAnyArgs().ListCoverCandidatesAsync(default!, default);
    }

    [Test]
    public async Task ListAsync_WithoutOwnCovers_PicksCoversOfTheTracks()
    {
        PlaylistRow mix = Row(Guid.NewGuid(), "Mix");
        PlaylistRow empty = Row(Guid.NewGuid(), "Empty");
        var track = Guid.NewGuid();
        PlaylistListQuery query = new(_userId, 0, 10);
        _playlists.ListAsync(query, Arg.Any<CancellationToken>()).Returns(new Page<PlaylistRow>([mix, empty], 0, 10, 2));
        _playlists.ListCoverCandidatesAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                _options.CoverScanEntries,
                Arg.Any<CancellationToken>())
            .Returns([new PlaylistCoverCandidateRow(mix.Id, 0, track, null, true)]);

        Page<PlaylistItem> page = await _service.ListAsync(query);

        page.Items[0].Cover.Should().BeEquivalentTo(new PlaylistCover(PlaylistCoverKind.Tracks, null, [track]));
        page.Items[1].Cover.Should().Be(PlaylistCover.None);
        page.Total.Should().Be(2);
    }

    [Test]
    public async Task ListEntriesAsync_MissingPlaylist_ReturnsNull()
    {
        Page<PlaylistEntryItem>? page = await _service.ListEntriesAsync(_userId, Guid.NewGuid(), 0, 10);

        page.Should().BeNull();
    }

    [Test]
    public async Task ListEntriesAsync_Always_AddsTheTracksAndLeavesOutRemovedOnes()
    {
        var playlistId = Guid.NewGuid();
        TrackRow track = Track("Song");
        PlaylistEntryRow kept = new(Guid.NewGuid(), track.Id, 0, _now);
        PlaylistEntryRow gone = new(Guid.NewGuid(), Guid.NewGuid(), 1, _now);
        _playlists.ListEntriesAsync(
                new PlaylistEntryListQuery(_userId, playlistId, 0, 10),
                Arg.Any<CancellationToken>())
            .Returns(new Page<PlaylistEntryRow>([kept, gone], 0, 10, 2));
        _tracks.ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns([track]);

        Page<PlaylistEntryItem>? page = await _service.ListEntriesAsync(_userId, playlistId, 0, 10);

        page!.Items.Should().Equal(new PlaylistEntryItem(kept, track));
    }

    [Test]
    public async Task GetEntryAsync_MissingEntryOfExistingPlaylist_ReturnsEntryNotFound()
    {
        var playlistId = Guid.NewGuid();
        _playlists.FindAsync(_userId, playlistId, Arg.Any<CancellationToken>()).Returns(Row(playlistId, "Mix"));

        PlaylistEntriesResult result = await _service.GetEntryAsync(_userId, playlistId, Guid.NewGuid());

        result.Status.Should().Be(PlaylistStatus.EntryNotFound);
    }

    [Test]
    public async Task GetEntryAsync_MissingPlaylist_ReturnsNotFound()
    {
        PlaylistEntriesResult result = await _service.GetEntryAsync(_userId, Guid.NewGuid(), Guid.NewGuid());

        result.Status.Should().Be(PlaylistStatus.NotFound);
    }

    [Test]
    public async Task AddEntriesAsync_Added_ReturnsTheEntriesAndThePlaylist()
    {
        var playlistId = Guid.NewGuid();
        TrackRow track = Track("Song");
        PlaylistEntryRow entry = new(Guid.NewGuid(), track.Id, 3, _now);
        _playlists.AddEntriesAsync(
                _userId,
                playlistId,
                Arg.Any<IReadOnlyList<Guid>>(),
                1,
                _options.MaxEntries,
                _now,
                Arg.Any<CancellationToken>())
            .Returns(new PlaylistEntryChange(PlaylistEntryChangeStatus.Done, [entry]));
        _tracks.ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns([track]);
        _playlists.FindAsync(_userId, playlistId, Arg.Any<CancellationToken>()).Returns(Row(playlistId, "Mix"));

        PlaylistEntriesResult result = await _service.AddEntriesAsync(_userId, playlistId, [track.Id], 1);

        result.IsOk.Should().BeTrue();
        result.Entries.Should().Equal(new PlaylistEntryItem(entry, track));
        result.Playlist!.Playlist.Id.Should().Be(playlistId);
    }

    [TestCase(PlaylistEntryChangeStatus.PlaylistNotFound, PlaylistStatus.NotFound)]
    [TestCase(PlaylistEntryChangeStatus.TrackNotFound, PlaylistStatus.TrackNotFound)]
    [TestCase(PlaylistEntryChangeStatus.Full, PlaylistStatus.Full)]
    [TestCase(PlaylistEntryChangeStatus.InvalidPosition, PlaylistStatus.InvalidPosition)]
    public async Task AddEntriesAsync_Refused_ReturnsTheStatusAndTheLimit(
        PlaylistEntryChangeStatus change,
        PlaylistStatus expected)
    {
        _playlists.AddEntriesAsync(
                default,
                default,
                default!,
                default,
                default,
                default,
                default)
            .ReturnsForAnyArgs(PlaylistEntryChange.Of(change, 7));

        PlaylistEntriesResult result = await _service.AddEntriesAsync(_userId, Guid.NewGuid(), [Guid.NewGuid()], null);

        result.Status.Should().Be(expected);
        result.Max.Should().Be(7);
    }

    [TestCase(PlaylistEntryChangeStatus.Done, PlaylistStatus.Ok)]
    [TestCase(PlaylistEntryChangeStatus.EntryNotFound, PlaylistStatus.EntryNotFound)]
    [TestCase(PlaylistEntryChangeStatus.PlaylistNotFound, PlaylistStatus.NotFound)]
    public async Task RemoveEntryAsync_Always_ReturnsTheStatus(PlaylistEntryChangeStatus change, PlaylistStatus expected)
    {
        _playlists.RemoveEntryAsync(default, default, default, default, default)
            .ReturnsForAnyArgs(PlaylistEntryChange.Of(change));

        PlaylistStatus status = await _service.RemoveEntryAsync(_userId, Guid.NewGuid(), Guid.NewGuid());

        status.Should().Be(expected);
    }

    [Test]
    public async Task MoveEntryAsync_Moved_ReturnsTheEntryAtItsNewPosition()
    {
        TrackRow track = Track("Song");
        PlaylistEntryRow moved = new(Guid.NewGuid(), track.Id, 0, _now);
        _playlists.MoveEntryAsync(default, default, default, default, default, default)
            .ReturnsForAnyArgs(new PlaylistEntryChange(PlaylistEntryChangeStatus.Done, [moved]));
        _tracks.ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns([track]);

        PlaylistEntriesResult result = await _service.MoveEntryAsync(_userId, Guid.NewGuid(), moved.Id, 0);

        result.Entries.Should().ContainSingle().Which.Entry.Position.Should().Be(0);
    }

    private static PlaylistRow Row(Guid id, string name, DateTimeOffset? coverUpdatedAt = null)
    {
        return new PlaylistRow(id, name, 0, 0, coverUpdatedAt, _now, _now);
    }

    private static TrackRow Track(string title)
    {
        return new TrackRow(
            Guid.NewGuid(),
            title,
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            null,
            null,
            null,
            null,
            TimeSpan.FromMinutes(3),
            true,
            false,
            false,
            _now);
    }
}
