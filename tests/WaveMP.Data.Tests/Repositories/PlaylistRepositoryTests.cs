using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Data.Tests.Repositories;

[TestFixture]
[Category("Integration")]
public class PlaylistRepositoryTests : LibraryDatabaseTests
{
    private static readonly DateTimeOffset _start = new(2026, 11, 1, 20, 0, 0, TimeSpan.Zero);

    protected override string DatabaseName => "wavemp_tests_playlists";

    [Test]
    public async Task CreateAsync_WithTracks_AddsThemInTheirOrder()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();

        Playlist playlist = NewPlaylist(user, "Road Trip");
        PlaylistEntryChange change = await CreateRepository().CreateAsync(
            playlist,
            [library.Ballad.Id, library.Opening.Id, library.Ballad.Id]);

        change.Status.Should().Be(PlaylistEntryChangeStatus.Done);
        change.Entries.Select(entry => entry.Position).Should().Equal(0, 1, 2);
        (await TrackIdsAsync(user, playlist)).Should().Equal(
            library.Ballad.Id,
            library.Opening.Id,
            library.Ballad.Id);
    }

    [Test]
    public async Task CreateAsync_UnknownTrack_CreatesNothing()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();

        Playlist playlist = NewPlaylist(user, "Broken");
        PlaylistEntryChange change = await CreateRepository().CreateAsync(
            playlist,
            [library.Opening.Id, Guid.NewGuid()]);

        change.Status.Should().Be(PlaylistEntryChangeStatus.TrackNotFound);
        (await CreateRepository().FindAsync(user.Id, playlist.Id)).Should().BeNull();
    }

    [Test]
    public async Task FindAsync_Always_SummarizesTheEntries()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        Playlist playlist = await CreateAsync(user, "Mix", library.Opening, library.Ballad, library.Opening);

        PlaylistRow? row = await CreateRepository().FindAsync(user.Id, playlist.Id);

        row.Should().NotBeNull();
        row!.Name.Should().Be("Mix");
        row.TrackCount.Should().Be(3);
        row.DurationMilliseconds.Should().Be(TimeSpan.FromMinutes(11).TotalMilliseconds);
        row.CoverUpdatedAt.Should().BeNull();
    }

    [Test]
    public async Task FindAsync_PlaylistOfAnotherUser_ReturnsNull()
    {
        await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        User other = await AddUserAsync();
        Playlist playlist = await CreateAsync(user, "Private");

        (await CreateRepository().FindAsync(other.Id, playlist.Id)).Should().BeNull();
    }

    [Test]
    public async Task ListAsync_ByName_SortsWithoutRegardToCaseAndOnlyListsTheUsersPlaylists()
    {
        await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        User other = await AddUserAsync();
        Playlist zebra = await CreateAsync(user, "zebra");
        Playlist alpha = await CreateAsync(user, "Alpha");
        Playlist middle = await CreateAsync(user, "Middle");
        await CreateAsync(other, "Foreign");

        Page<PlaylistRow> page = await CreateRepository().ListAsync(new PlaylistListQuery(user.Id, 0, 10));

        page.Items.Select(row => row.Id).Should().Equal(alpha.Id, middle.Id, zebra.Id);
        page.Total.Should().Be(3);
    }

    [Test]
    public async Task ListAsync_ByCreatedAtDescending_ReturnsTheNewestFirst()
    {
        await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        Playlist first = await CreateAsync(user, "B", createdAt: _start);
        Playlist second = await CreateAsync(user, "A", createdAt: _start.AddMinutes(1));

        Page<PlaylistRow> page = await CreateRepository().ListAsync(
            new PlaylistListQuery(user.Id, 0, 10, PlaylistSortField.CreatedAt, SortDirection.Desc));

        page.Items.Select(row => row.Id).Should().Equal(second.Id, first.Id);
    }

    [Test]
    public async Task ListAsync_WithSearch_FindsNamesContainingTheText()
    {
        await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        Playlist party = await CreateAsync(user, "Summer Party");
        await CreateAsync(user, "Rainy Day");

        Page<PlaylistRow> page = await CreateRepository().ListAsync(
            new PlaylistListQuery(user.Id, 0, 10, Search: "party"));

        page.Items.Should().ContainSingle().Which.Id.Should().Be(party.Id);
    }

    [Test]
    public async Task AddEntriesAsync_WithoutPosition_AppendsTheTracks()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        Playlist playlist = await CreateAsync(user, "Mix", library.Opening);

        PlaylistEntryChange change = await CreateRepository().AddEntriesAsync(
            user.Id,
            playlist.Id,
            [library.Ballad.Id, library.Opening.Id],
            null,
            100,
            _start);

        change.Status.Should().Be(PlaylistEntryChangeStatus.Done);
        change.Entries.Select(entry => entry.Position).Should().Equal(1, 2);
        (await TrackIdsAsync(user, playlist)).Should().Equal(
            library.Opening.Id,
            library.Ballad.Id,
            library.Opening.Id);
    }

    [Test]
    public async Task AddEntriesAsync_AtPosition_MovesTheFollowingEntriesBack()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        Playlist playlist = await CreateAsync(user, "Mix", library.Opening, library.Closing);

        PlaylistEntryChange change = await CreateRepository().AddEntriesAsync(
            user.Id,
            playlist.Id,
            [library.Ballad.Id, library.Duet.Id],
            1,
            100,
            _start);

        change.Entries.Select(entry => entry.Position).Should().Equal(1, 2);
        (await TrackIdsAsync(user, playlist)).Should().Equal(
            library.Opening.Id,
            library.Ballad.Id,
            library.Duet.Id,
            library.Closing.Id);
    }

    [Test]
    public async Task AddEntriesAsync_PositionAfterTheEnd_ReturnsInvalidPositionWithTheLargestPosition()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        Playlist playlist = await CreateAsync(user, "Mix", library.Opening, library.Closing);

        PlaylistEntryChange change = await CreateRepository().AddEntriesAsync(
            user.Id,
            playlist.Id,
            [library.Ballad.Id],
            3,
            100,
            _start);

        change.Status.Should().Be(PlaylistEntryChangeStatus.InvalidPosition);
        change.Max.Should().Be(2);
        (await TrackIdsAsync(user, playlist)).Should().HaveCount(2);
    }

    [Test]
    public async Task AddEntriesAsync_MoreThanTheLimit_ReturnsFull()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        Playlist playlist = await CreateAsync(user, "Mix", library.Opening, library.Closing);

        PlaylistEntryChange change = await CreateRepository().AddEntriesAsync(
            user.Id,
            playlist.Id,
            [library.Ballad.Id, library.Duet.Id],
            null,
            3,
            _start);

        change.Status.Should().Be(PlaylistEntryChangeStatus.Full);
        change.Max.Should().Be(3);
        (await TrackIdsAsync(user, playlist)).Should().HaveCount(2);
    }

    [Test]
    public async Task AddEntriesAsync_UnknownTrack_AddsNothing()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        Playlist playlist = await CreateAsync(user, "Mix", library.Opening);
        DateTimeOffset updatedAt = (await CreateRepository().FindAsync(user.Id, playlist.Id))!.UpdatedAt;

        PlaylistEntryChange change = await CreateRepository().AddEntriesAsync(
            user.Id,
            playlist.Id,
            [library.Ballad.Id, Guid.NewGuid()],
            null,
            100,
            _start.AddDays(1));

        change.Status.Should().Be(PlaylistEntryChangeStatus.TrackNotFound);
        (await TrackIdsAsync(user, playlist)).Should().Equal(library.Opening.Id);
        (await CreateRepository().FindAsync(user.Id, playlist.Id))!.UpdatedAt.Should().Be(updatedAt);
    }

    [Test]
    public async Task AddEntriesAsync_PlaylistOfAnotherUser_ReturnsPlaylistNotFound()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        User other = await AddUserAsync();
        Playlist playlist = await CreateAsync(user, "Mix");

        PlaylistEntryChange change = await CreateRepository().AddEntriesAsync(
            other.Id,
            playlist.Id,
            [library.Ballad.Id],
            null,
            100,
            _start);

        change.Status.Should().Be(PlaylistEntryChangeStatus.PlaylistNotFound);
    }

    [TestCase(3, 0, new[] { 3, 0, 1, 2 })]
    [TestCase(0, 3, new[] { 1, 2, 3, 0 })]
    [TestCase(1, 2, new[] { 0, 2, 1, 3 })]
    [TestCase(2, 2, new[] { 0, 1, 2, 3 })]
    public async Task MoveEntryAsync_ToPosition_MovesTheEntriesInBetween(int from, int to, int[] order)
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        Track[] tracks = [library.Opening, library.Closing, library.Duet, library.Ballad];
        Playlist playlist = await CreateAsync(user, "Mix", tracks);
        Guid entryId = (await EntriesAsync(user, playlist))[from].Id;

        PlaylistEntryChange change = await CreateRepository().MoveEntryAsync(
            user.Id,
            playlist.Id,
            entryId,
            to,
            _start);

        change.Status.Should().Be(PlaylistEntryChangeStatus.Done);
        change.Entries.Should().ContainSingle().Which.Position.Should().Be(to);
        (await TrackIdsAsync(user, playlist)).Should().Equal(order.Select(index => tracks[index].Id));
        (await EntriesAsync(user, playlist)).Select(entry => entry.Position).Should().Equal(0, 1, 2, 3);
    }

    [Test]
    public async Task MoveEntryAsync_BehindTheLastEntry_ReturnsInvalidPositionWithTheLastPosition()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        Playlist playlist = await CreateAsync(user, "Mix", library.Opening, library.Closing);
        Guid entryId = (await EntriesAsync(user, playlist))[0].Id;

        PlaylistEntryChange change = await CreateRepository().MoveEntryAsync(
            user.Id,
            playlist.Id,
            entryId,
            2,
            _start);

        change.Status.Should().Be(PlaylistEntryChangeStatus.InvalidPosition);
        change.Max.Should().Be(1);
    }

    [Test]
    public async Task MoveEntryAsync_EntryOfAnotherPlaylist_ReturnsEntryNotFound()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        Playlist playlist = await CreateAsync(user, "Mix", library.Opening);
        Playlist other = await CreateAsync(user, "Other", library.Closing);
        Guid foreignEntry = (await EntriesAsync(user, other))[0].Id;

        PlaylistEntryChange change = await CreateRepository().MoveEntryAsync(
            user.Id,
            playlist.Id,
            foreignEntry,
            0,
            _start);

        change.Status.Should().Be(PlaylistEntryChangeStatus.EntryNotFound);
    }

    [Test]
    public async Task RemoveEntryAsync_Always_ClosesTheGap()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        Playlist playlist = await CreateAsync(user, "Mix", library.Opening, library.Closing, library.Duet);
        Guid entryId = (await EntriesAsync(user, playlist))[1].Id;

        PlaylistEntryChange change = await CreateRepository().RemoveEntryAsync(user.Id, playlist.Id, entryId, _start);

        change.Status.Should().Be(PlaylistEntryChangeStatus.Done);
        (await TrackIdsAsync(user, playlist)).Should().Equal(library.Opening.Id, library.Duet.Id);
        (await EntriesAsync(user, playlist)).Select(entry => entry.Position).Should().Equal(0, 1);
        (await CreateRepository().FindEntryAsync(user.Id, playlist.Id, entryId)).Should().BeNull();
    }

    [Test]
    public async Task DeletingATrack_Always_RemovesItsEntriesAndTheNextChangeClosesTheGaps()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        Playlist playlist = await CreateAsync(
            user,
            "Mix",
            library.Opening,
            library.Closing,
            library.Opening,
            library.Duet);
        await using (WaveMpDbContext dbContext = CreateDbContext())
        {
            await dbContext.Tracks.Where(track => track.Id == library.Opening.Id).ExecuteDeleteAsync();
        }

        IReadOnlyList<PlaylistEntryRow> entries = await EntriesAsync(user, playlist);
        entries.Select(entry => entry.TrackId).Should().Equal(library.Closing.Id, library.Duet.Id);
        entries.Select(entry => entry.Position).Should().Equal(0, 1);
        (await CreateRepository().FindEntryAsync(user.Id, playlist.Id, entries[1].Id))!.Position.Should().Be(1);

        PlaylistEntryChange change = await CreateRepository().AddEntriesAsync(
            user.Id,
            playlist.Id,
            [library.Ballad.Id],
            null,
            100,
            _start);

        change.Entries.Should().ContainSingle().Which.Position.Should().Be(2);
        await using WaveMpDbContext context = CreateDbContext();
        List<int> stored = await context.PlaylistEntries
            .Where(entry => entry.PlaylistId == playlist.Id)
            .OrderBy(entry => entry.Position)
            .Select(entry => entry.Position)
            .ToListAsync();
        stored.Should().Equal(0, 1, 2);
    }

    [Test]
    public async Task ListEntriesAsync_WithOffset_NumbersThePositionsOfThePage()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        Playlist playlist = await CreateAsync(user, "Mix", library.Opening, library.Closing, library.Duet);

        Page<PlaylistEntryRow>? page = await CreateRepository().ListEntriesAsync(
            new PlaylistEntryListQuery(user.Id, playlist.Id, 1, 1));

        page!.Items.Should().ContainSingle().Which.Should().Match<PlaylistEntryRow>(entry =>
            entry.TrackId == library.Closing.Id && entry.Position == 1);
        page.Total.Should().Be(3);
    }

    [Test]
    public async Task ListEntriesAsync_PlaylistOfAnotherUser_ReturnsNull()
    {
        await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        User other = await AddUserAsync();
        Playlist playlist = await CreateAsync(user, "Mix");

        (await CreateRepository().ListEntriesAsync(new PlaylistEntryListQuery(other.Id, playlist.Id, 0, 10)))
            .Should().BeNull();
    }

    [Test]
    public async Task RenameAsync_Always_ChangesTheNameAndTheTimeOfChange()
    {
        await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        User other = await AddUserAsync();
        Playlist playlist = await CreateAsync(user, "Old");
        DateTimeOffset now = _start.AddDays(2);

        IPlaylistRepository repository = CreateRepository();
        bool foreign = await repository.RenameAsync(other.Id, playlist.Id, "Stolen", "STOLEN", now);
        bool renamed = await repository.RenameAsync(user.Id, playlist.Id, "New", "NEW", now);

        foreign.Should().BeFalse();
        renamed.Should().BeTrue();
        PlaylistRow row = (await CreateRepository().FindAsync(user.Id, playlist.Id))!;
        row.Name.Should().Be("New");
        row.UpdatedAt.Should().Be(now);
    }

    [Test]
    public async Task SetCoverAsync_Always_StoresTheTimeOfTheUpload()
    {
        await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        Playlist playlist = await CreateAsync(user, "Mix");
        DateTimeOffset now = _start.AddDays(1);

        (await CreateRepository().SetCoverAsync(user.Id, playlist.Id, now, now)).Should().BeTrue();
        (await CreateRepository().FindAsync(user.Id, playlist.Id))!.CoverUpdatedAt.Should().Be(now);

        (await CreateRepository().SetCoverAsync(user.Id, playlist.Id, null, now)).Should().BeTrue();
        (await CreateRepository().FindAsync(user.Id, playlist.Id))!.CoverUpdatedAt.Should().BeNull();
    }

    [Test]
    public async Task DeleteAsync_Always_DeletesTheEntries()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        User other = await AddUserAsync();
        Playlist playlist = await CreateAsync(user, "Mix", library.Opening);

        (await CreateRepository().DeleteAsync(other.Id, playlist.Id)).Should().BeFalse();
        (await CreateRepository().DeleteAsync(user.Id, playlist.Id)).Should().BeTrue();

        await using WaveMpDbContext dbContext = CreateDbContext();
        (await dbContext.PlaylistEntries.AnyAsync(entry => entry.PlaylistId == playlist.Id)).Should().BeFalse();
    }

    [Test]
    public async Task DeletingTheUser_Always_DeletesItsPlaylists()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        Playlist playlist = await CreateAsync(user, "Mix", library.Opening);

        await using (WaveMpDbContext dbContext = CreateDbContext())
        {
            await dbContext.Users.Where(candidate => candidate.Id == user.Id).ExecuteDeleteAsync();
        }

        (await CreateRepository().ListExistingIdsAsync([playlist.Id])).Should().BeEmpty();
    }

    [Test]
    public async Task ListCoverCandidatesAsync_Always_ReturnsTheFirstEntriesOfEachPlaylistWithTheirCovers()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        Playlist first = await CreateAsync(user, "First", library.HitOne, library.Opening, library.Ballad);
        Playlist second = await CreateAsync(user, "Second", library.LooseSong, library.Closing);

        IReadOnlyList<PlaylistCoverCandidateRow> candidates = await CreateRepository().ListCoverCandidatesAsync(
            [first.Id, second.Id],
            2);

        candidates.Where(candidate => candidate.PlaylistId == first.Id)
            .OrderBy(candidate => candidate.Position)
            .Select(candidate => (candidate.TrackId, candidate.AlbumId, candidate.HasCover))
            .Should().Equal(
                (library.HitOne.Id, library.Hits.Id, false),
                (library.Opening.Id, library.FirstAlbum.Id, true));
        candidates.Where(candidate => candidate.PlaylistId == second.Id)
            .OrderBy(candidate => candidate.Position)
            .Select(candidate => (candidate.TrackId, candidate.HasCover))
            .Should().Equal((library.LooseSong.Id, false), (library.Closing.Id, true));
    }

    [Test]
    public async Task ListExistingIdsAsync_Always_LeavesOutMissingPlaylists()
    {
        await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        Playlist playlist = await CreateAsync(user, "Mix");

        IReadOnlySet<Guid> existing = await CreateRepository().ListExistingIdsAsync([playlist.Id, Guid.NewGuid()]);

        existing.Should().BeEquivalentTo([playlist.Id]);
    }

    private IPlaylistRepository CreateRepository()
    {
        return new PlaylistRepository(CreateDbContext());
    }

    private static Playlist NewPlaylist(User user, string name, DateTimeOffset? createdAt = null)
    {
        return new Playlist
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            CreatedAt = createdAt ?? _start,
            UpdatedAt = createdAt ?? _start,
        };
    }

    private Task<Playlist> CreateAsync(User user, string name, params Track[] tracks)
    {
        return CreateAsync(user, name, null, tracks);
    }

    private async Task<Playlist> CreateAsync(
        User user,
        string name,
        DateTimeOffset? createdAt,
        params Track[] tracks)
    {
        Playlist playlist = NewPlaylist(user, name, createdAt);
        PlaylistEntryChange change = await CreateRepository().CreateAsync(
            playlist,
            [.. tracks.Select(track => track.Id)]);
        change.Status.Should().Be(PlaylistEntryChangeStatus.Done);
        return playlist;
    }

    private async Task<IReadOnlyList<PlaylistEntryRow>> EntriesAsync(User user, Playlist playlist)
    {
        Page<PlaylistEntryRow>? page = await CreateRepository().ListEntriesAsync(
            new PlaylistEntryListQuery(user.Id, playlist.Id, 0, 100));
        return page!.Items;
    }

    private async Task<IReadOnlyList<Guid>> TrackIdsAsync(User user, Playlist playlist)
    {
        return [.. (await EntriesAsync(user, playlist)).Select(entry => entry.TrackId)];
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
}
