using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Data.Tests.Repositories;

[TestFixture]
[Category("Integration")]
public class PlayHistoryRepositoryTests : LibraryDatabaseTests
{
    private static readonly DateTimeOffset _start = new(2026, 11, 1, 20, 0, 0, TimeSpan.Zero);

    protected override string DatabaseName => "wavemp_tests_play_history";

    [Test]
    public async Task ListAsync_Always_ReturnsTheNewestPlayFirst()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        PlayHistoryEntry older = await AddEntryAsync(user, library.Opening, _start);
        PlayHistoryEntry newer = await AddEntryAsync(user, library.Ballad, _start.AddMinutes(5));
        PlayHistoryEntry newest = await AddEntryAsync(user, library.Opening, _start.AddMinutes(10));

        Page<PlayHistoryEntry> page = await CreateRepository().ListAsync(new PlayHistoryListQuery(user.Id, 0, 10));

        page.Items.Select(entry => entry.Id).Should().Equal(newest.Id, newer.Id, older.Id);
        page.Total.Should().Be(3);
    }

    [Test]
    public async Task ListAsync_WithOffsetAndLimit_ReturnsOnePage()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        List<PlayHistoryEntry> entries = [];
        for (int index = 0; index < 5; index++)
        {
            entries.Add(await AddEntryAsync(user, library.Opening, _start.AddMinutes(index)));
        }

        Page<PlayHistoryEntry> page = await CreateRepository().ListAsync(new PlayHistoryListQuery(user.Id, 1, 2));

        page.Items.Select(entry => entry.Id).Should().Equal(entries[3].Id, entries[2].Id);
        page.Total.Should().Be(5);
        page.Offset.Should().Be(1);
        page.Limit.Should().Be(2);
    }

    [Test]
    public async Task ListAsync_Always_OnlyReturnsTheEntriesOfTheUser()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        User other = await AddUserAsync();
        PlayHistoryEntry own = await AddEntryAsync(user, library.Opening, _start);
        await AddEntryAsync(other, library.Opening, _start);

        Page<PlayHistoryEntry> page = await CreateRepository().ListAsync(new PlayHistoryListQuery(user.Id, 0, 10));

        page.Items.Should().ContainSingle().Which.Id.Should().Be(own.Id);
    }

    [Test]
    public async Task FindAsync_EntryOfAnotherUser_ReturnsNull()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        User other = await AddUserAsync();
        PlayHistoryEntry entry = await AddEntryAsync(user, library.Opening, _start);

        IPlayHistoryRepository repository = CreateRepository();
        PlayHistoryEntry? own = await repository.FindAsync(user.Id, entry.Id);
        PlayHistoryEntry? foreign = await repository.FindAsync(other.Id, entry.Id);

        own.Should().NotBeNull();
        own!.TrackId.Should().Be(library.Opening.Id);
        own.PlayedAt.Should().Be(_start);
        foreign.Should().BeNull();
    }

    [Test]
    public async Task DeletingTheUser_Always_DeletesItsHistory()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        PlayHistoryEntry entry = await AddEntryAsync(user, library.Opening, _start);

        await using (WaveMpDbContext dbContext = CreateDbContext())
        {
            await dbContext.Users.Where(candidate => candidate.Id == user.Id).ExecuteDeleteAsync();
        }

        (await CreateRepository().FindAsync(user.Id, entry.Id)).Should().BeNull();
    }

    [Test]
    public async Task DeletingTheTrack_Always_DeletesItsHistory()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        PlayHistoryEntry removed = await AddEntryAsync(user, library.Opening, _start);
        PlayHistoryEntry kept = await AddEntryAsync(user, library.Ballad, _start);

        await using (WaveMpDbContext dbContext = CreateDbContext())
        {
            await dbContext.Tracks.Where(track => track.Id == library.Opening.Id).ExecuteDeleteAsync();
        }

        Page<PlayHistoryEntry> page = await CreateRepository().ListAsync(new PlayHistoryListQuery(user.Id, 0, 10));
        page.Items.Select(entry => entry.Id).Should().Equal(kept.Id);
        page.Items.Should().NotContain(entry => entry.Id == removed.Id);
    }

    [Test]
    public async Task ListRecentTracksAsync_Always_ListsEachTrackOnceByItsLastPlay()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        User other = await AddUserAsync();
        await AddEntryAsync(user, library.Opening, _start);
        await AddEntryAsync(user, library.Ballad, _start.AddMinutes(5));
        await AddEntryAsync(user, library.Opening, _start.AddMinutes(10));
        await AddEntryAsync(user, library.Duet, _start.AddMinutes(1));
        await AddEntryAsync(other, library.Closing, _start.AddMinutes(20));

        Page<RecentTrackRow> page = await CreateRepository().ListRecentTracksAsync(
            new RecentTrackListQuery(user.Id, 0, 10, 100));

        page.Items.Should().Equal(
            new RecentTrackRow(library.Opening.Id, _start.AddMinutes(10)),
            new RecentTrackRow(library.Ballad.Id, _start.AddMinutes(5)),
            new RecentTrackRow(library.Duet.Id, _start.AddMinutes(1)));
        page.Total.Should().Be(3);
    }

    [Test]
    public async Task ListRecentTracksAsync_MoreTracksThanTheMaximum_CutsTheList()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        await AddEntryAsync(user, library.Opening, _start);
        await AddEntryAsync(user, library.Ballad, _start.AddMinutes(1));
        await AddEntryAsync(user, library.Duet, _start.AddMinutes(2));

        IPlayHistoryRepository repository = CreateRepository();
        Page<RecentTrackRow> first = await repository.ListRecentTracksAsync(new RecentTrackListQuery(user.Id, 0, 1, 2));
        Page<RecentTrackRow> second = await repository.ListRecentTracksAsync(new RecentTrackListQuery(user.Id, 1, 5, 2));
        Page<RecentTrackRow> beyond = await repository.ListRecentTracksAsync(new RecentTrackListQuery(user.Id, 2, 5, 2));

        first.Items.Select(row => row.TrackId).Should().Equal(library.Duet.Id);
        second.Items.Select(row => row.TrackId).Should().Equal(library.Ballad.Id);
        beyond.Items.Should().BeEmpty();
        first.Total.Should().Be(2);
        beyond.Total.Should().Be(2);
    }

    private IPlayHistoryRepository CreateRepository()
    {
        return new PlayHistoryRepository(CreateDbContext());
    }

    private async Task<User> AddUserAsync()
    {
        string name = "Listener " + Guid.NewGuid().ToString("N")[..8];
        User user = new()
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            Initials = "L",
            Color = "#336699",
            Role = UserRole.User,
            IsBuiltIn = false,
            CreatedAt = _start,
        };
        await using WaveMpDbContext dbContext = CreateDbContext();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    private async Task<PlayHistoryEntry> AddEntryAsync(User user, Track track, DateTimeOffset playedAt)
    {
        PlayHistoryEntry entry = new()
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            TrackId = track.Id,
            PlayedAt = playedAt,
        };
        await CreateRepository().AddAsync(entry);
        return entry;
    }
}
