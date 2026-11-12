using AwesomeAssertions;
using NSubstitute;
using WaveMP.Core.Playlists.Histories;
using WaveMP.Core.Playlists.Options;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Playlists.Tests.Histories;

[TestFixture]
public class RecentlyPlayedServiceTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 12, 20, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task ListAsync_Always_ReadsTheConfiguredMaximumAndAddsTheTracks()
    {
        var userId = Guid.NewGuid();
        TrackRow track = new(
            Guid.NewGuid(),
            "Song",
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
            null,
            true,
            false,
            false,
            _now);
        IPlayHistoryRepository history = Substitute.For<IPlayHistoryRepository>();
        history.ListRecentTracksAsync(new RecentTrackListQuery(userId, 5, 10, 42), Arg.Any<CancellationToken>())
            .Returns(new Page<RecentTrackRow>(
                [new RecentTrackRow(track.Id, _now), new RecentTrackRow(Guid.NewGuid(), _now.AddMinutes(-1))],
                5,
                10,
                42));
        ITrackRepository tracks = Substitute.For<ITrackRepository>();
        tracks.ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), userId, Arg.Any<CancellationToken>())
            .Returns([track]);
        RecentlyPlayedService service = new(
            history,
            tracks,
            Microsoft.Extensions.Options.Options.Create(new PlaylistOptions { RecentlyPlayedMaxTracks = 42 }));

        Page<RecentlyPlayedItem> page = await service.ListAsync(userId, 5, 10);

        page.Items.Should().Equal(new RecentlyPlayedItem(track, _now));
        page.Total.Should().Be(42);
    }
}
