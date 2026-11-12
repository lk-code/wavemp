using AwesomeAssertions;
using NSubstitute;
using WaveMP.Core.Streaming.Histories;
using WaveMP.Core.Streaming.Options;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Streaming.Tests.Histories;

[TestFixture]
public class PlayHistoryServiceTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 11, 20, 0, 0, TimeSpan.Zero);

    private readonly Guid _userId = Guid.NewGuid();

    private IPlayHistoryRepository _history = null!;
    private ITrackRepository _tracks = null!;
    private PlayHistoryService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _history = Substitute.For<IPlayHistoryRepository>();
        _tracks = Substitute.For<ITrackRepository>();
        _tracks.FindAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns((TrackRow?)null);
        TimeProvider timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(_now);
        _service = new PlayHistoryService(
            _history,
            _tracks,
            timeProvider,
            Microsoft.Extensions.Options.Options.Create(new StreamingOptions()));
    }

    [Test]
    public async Task RecordAsync_WithStart_SavesTheEntryForTheUser()
    {
        TrackRow track = Track("Song");
        DateTimeOffset start = _now.AddMinutes(-3);

        PlayHistoryResult result = await _service.RecordAsync(_userId, track.Id, start);

        result.Status.Should().Be(PlayHistoryStatus.Created);
        result.Item!.Track.Should().Be(track);
        result.Item.Entry.UserId.Should().Be(_userId);
        result.Item.Entry.TrackId.Should().Be(track.Id);
        result.Item.Entry.PlayedAt.Should().Be(start);
        result.Item.Entry.Id.Should().NotBeEmpty();
        await _history.Received(1).AddAsync(result.Item.Entry, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RecordAsync_WithoutStart_UsesNow()
    {
        TrackRow track = Track("Song");

        PlayHistoryResult result = await _service.RecordAsync(_userId, track.Id, null);

        result.Item!.Entry.PlayedAt.Should().Be(_now);
    }

    [Test]
    public async Task RecordAsync_StartWithTicks_KeepsMicroseconds()
    {
        TrackRow track = Track("Song");
        DateTimeOffset start = _now.AddMinutes(-3).AddTicks(1_234_567);

        PlayHistoryResult result = await _service.RecordAsync(_userId, track.Id, start);

        result.Item!.Entry.PlayedAt.Should().Be(_now.AddMinutes(-3).AddTicks(1_234_560));
    }

    [Test]
    public async Task RecordAsync_StartWithinTheClockSkew_IsAccepted()
    {
        TrackRow track = Track("Song");

        PlayHistoryResult result = await _service.RecordAsync(_userId, track.Id, _now.AddSeconds(30));

        result.Status.Should().Be(PlayHistoryStatus.Created);
    }

    [Test]
    public async Task RecordAsync_StartInTheFuture_IsRejected()
    {
        TrackRow track = Track("Song");

        PlayHistoryResult result = await _service.RecordAsync(_userId, track.Id, _now.AddMinutes(5));

        result.Status.Should().Be(PlayHistoryStatus.InvalidPlayedAt);
        await _history.DidNotReceive().AddAsync(Arg.Any<PlayHistoryEntry>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RecordAsync_StartOlderThanTheMaximumAge_IsRejected()
    {
        TrackRow track = Track("Song");

        PlayHistoryResult result = await _service.RecordAsync(_userId, track.Id, _now.AddDays(-8));

        result.Status.Should().Be(PlayHistoryStatus.InvalidPlayedAt);
    }

    [Test]
    public async Task RecordAsync_UnknownTrack_IsRejected()
    {
        PlayHistoryResult result = await _service.RecordAsync(_userId, Guid.NewGuid(), null);

        result.Status.Should().Be(PlayHistoryStatus.TrackNotFound);
        await _history.DidNotReceive().AddAsync(Arg.Any<PlayHistoryEntry>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ListAsync_Always_AddsTheTracksInTheOrderOfTheHistory()
    {
        TrackRow first = Track("First");
        TrackRow second = Track("Second");
        PlayHistoryEntry newest = Entry(second, _now);
        PlayHistoryEntry older = Entry(first, _now.AddMinutes(-5));
        PlayHistoryEntry gone = Entry(TrackRowOf(Guid.NewGuid(), "Gone"), _now.AddMinutes(-10));
        _history.ListAsync(new PlayHistoryListQuery(_userId, 0, 10), Arg.Any<CancellationToken>())
            .Returns(new Page<PlayHistoryEntry>([newest, older, gone], 0, 10, 3));
        _tracks.ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), _userId, Arg.Any<CancellationToken>())
            .Returns([first, second]);

        Page<PlayHistoryItem> page = await _service.ListAsync(_userId, 0, 10);

        page.Items.Select(item => item.Track.Title).Should().Equal("Second", "First");
        page.Items.Select(item => item.Entry).Should().Equal(newest, older);
        page.Total.Should().Be(3);
    }

    [Test]
    public async Task GetAsync_EntryOfTheUser_ReturnsItWithItsTrack()
    {
        TrackRow track = Track("Song");
        PlayHistoryEntry entry = Entry(track, _now);
        _history.FindAsync(_userId, entry.Id, Arg.Any<CancellationToken>()).Returns(entry);

        PlayHistoryItem? item = await _service.GetAsync(_userId, entry.Id);

        item.Should().Be(new PlayHistoryItem(entry, track));
    }

    [Test]
    public async Task GetAsync_UnknownEntry_ReturnsNull()
    {
        _history.FindAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((PlayHistoryEntry?)null);

        PlayHistoryItem? item = await _service.GetAsync(_userId, Guid.NewGuid());

        item.Should().BeNull();
    }

    private TrackRow Track(string title)
    {
        TrackRow track = TrackRowOf(Guid.NewGuid(), title);
        _tracks.FindAsync(track.Id, _userId, Arg.Any<CancellationToken>()).Returns(track);
        return track;
    }

    private static TrackRow TrackRowOf(Guid id, string title)
    {
        return new TrackRow(
            id,
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

    private PlayHistoryEntry Entry(TrackRow track, DateTimeOffset playedAt)
    {
        return new PlayHistoryEntry
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            TrackId = track.Id,
            PlayedAt = playedAt,
        };
    }
}
