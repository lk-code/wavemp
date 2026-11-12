using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;
using WaveMP.Testing.Seeds;

namespace WaveMP.Data.Tests.Repositories;

[TestFixture]
[Category("Integration")]
public class FavoriteRepositoryTests : LibraryDatabaseTests
{
    private static readonly DateTimeOffset _start = new(2026, 11, 1, 20, 0, 0, TimeSpan.Zero);

    protected override string DatabaseName => "wavemp_tests_favorites";

    [TestCase(FavoriteType.Track)]
    [TestCase(FavoriteType.Album)]
    [TestCase(FavoriteType.Artist)]
    public async Task AddAsync_NewTarget_AddsIt(FavoriteType type)
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        Guid itemId = ItemOf(library, type);

        FavoriteChangeStatus status = await CreateRepository().AddAsync(NewFavorite(user, type, itemId, _start));

        status.Should().Be(FavoriteChangeStatus.Added);
        Page<FavoriteRow> page = await CreateRepository().ListAsync(new FavoriteListQuery(user.Id, 0, 10));
        page.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new { Type = type, ItemId = itemId, CreatedAt = _start });
    }

    [Test]
    public async Task AddAsync_TargetMarkedBefore_KeepsTheFirstFavorite()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        Favorite first = NewFavorite(user, FavoriteType.Track, library.Ballad.Id, _start);
        await CreateRepository().AddAsync(first);

        FavoriteChangeStatus status = await CreateRepository().AddAsync(
            NewFavorite(user, FavoriteType.Track, library.Ballad.Id, _start.AddHours(1)));

        status.Should().Be(FavoriteChangeStatus.Existing);
        Page<FavoriteRow> page = await CreateRepository().ListAsync(new FavoriteListQuery(user.Id, 0, 10));
        page.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new { first.Id, CreatedAt = _start });
    }

    [Test]
    public async Task AddAsync_SameTargetOfAnotherUser_AddsItForBoth()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        User other = await AddUserAsync();

        FavoriteChangeStatus first = await CreateRepository().AddAsync(
            NewFavorite(user, FavoriteType.Album, library.FirstAlbum.Id, _start));
        FavoriteChangeStatus second = await CreateRepository().AddAsync(
            NewFavorite(other, FavoriteType.Album, library.FirstAlbum.Id, _start));

        first.Should().Be(FavoriteChangeStatus.Added);
        second.Should().Be(FavoriteChangeStatus.Added);
    }

    [TestCase(FavoriteType.Track)]
    [TestCase(FavoriteType.Album)]
    [TestCase(FavoriteType.Artist)]
    public async Task AddAsync_UnknownTarget_ReturnsTargetNotFound(FavoriteType type)
    {
        await SeedStandardLibraryAsync();
        User user = await AddUserAsync();

        FavoriteChangeStatus status = await CreateRepository().AddAsync(
            NewFavorite(user, type, Guid.NewGuid(), _start));

        status.Should().Be(FavoriteChangeStatus.TargetNotFound);
        (await CreateRepository().ListAsync(new FavoriteListQuery(user.Id, 0, 10))).Total.Should().Be(0);
    }

    [Test]
    public async Task AddAsync_IdOfAnotherType_Throws()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        Favorite favorite = NewFavorite(user, FavoriteType.Track, library.Ballad.Id, _start);
        favorite.AlbumId = library.SecondAlbum.Id;

        Func<Task> add = () => CreateRepository().AddAsync(favorite);

        await add.Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    public async Task RemoveAsync_Favorite_RemovesOnlyIt()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        User other = await AddUserAsync();
        LibrarySeed seed = new();
        seed.Favorite(user, library.Ballad);
        seed.Favorite(user, library.SecondAlbum);
        seed.Favorite(other, library.Ballad);
        await SaveAsync(seed);

        bool removed = await CreateRepository().RemoveAsync(user.Id, FavoriteType.Track, library.Ballad.Id);
        bool again = await CreateRepository().RemoveAsync(user.Id, FavoriteType.Track, library.Ballad.Id);

        removed.Should().BeTrue();
        again.Should().BeFalse();
        (await ItemIdsAsync(user)).Should().Equal(library.SecondAlbum.Id);
        (await ItemIdsAsync(other)).Should().Equal(library.Ballad.Id);
    }

    [Test]
    public async Task RemoveAsync_OtherType_RemovesNothing()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        LibrarySeed seed = new();
        seed.Favorite(user, library.Alpha);
        await SaveAsync(seed);

        bool removed = await CreateRepository().RemoveAsync(user.Id, FavoriteType.Album, library.Alpha.Id);

        removed.Should().BeFalse();
        (await ItemIdsAsync(user)).Should().Equal(library.Alpha.Id);
    }

    [Test]
    public async Task ListAsync_Always_ReturnsTheNewestFirstAndFiltersByType()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        LibrarySeed seed = new();
        seed.Favorite(user, library.Opening, _start);
        seed.Favorite(user, library.FirstAlbum, _start.AddMinutes(1));
        seed.Favorite(user, library.Beta, _start.AddMinutes(2));
        seed.Favorite(user, library.Ballad, _start.AddMinutes(3));
        await SaveAsync(seed);

        Page<FavoriteRow> all = await CreateRepository().ListAsync(new FavoriteListQuery(user.Id, 0, 10));
        Page<FavoriteRow> tracks = await CreateRepository().ListAsync(
            new FavoriteListQuery(user.Id, 0, 1, FavoriteType.Track));

        all.Items.Select(row => row.ItemId).Should().Equal(
            library.Ballad.Id,
            library.Beta.Id,
            library.FirstAlbum.Id,
            library.Opening.Id);
        all.Items.Select(row => row.Type).Should().Equal(
            FavoriteType.Track,
            FavoriteType.Artist,
            FavoriteType.Album,
            FavoriteType.Track);
        tracks.Items.Select(row => row.ItemId).Should().Equal(library.Ballad.Id);
        tracks.Total.Should().Be(2);
    }

    [Test]
    public async Task DeleteTargets_WithExecuteDelete_RemovesTheirFavorites()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        LibrarySeed seed = new();
        seed.Favorite(user, library.LooseSong);
        seed.Favorite(user, library.Hits);
        seed.Favorite(user, library.Guest);
        seed.Favorite(user, library.Ballad);
        await SaveAsync(seed);

        await using (WaveMpDbContext dbContext = CreateDbContext())
        {
            // Like the scan: the tracks first, then the albums and artists nothing refers to any longer.
            await dbContext.Tracks
                .Where(track => track.Id == library.LooseSong.Id
                    || track.Id == library.Duet.Id
                    || track.AlbumId == library.Hits.Id)
                .ExecuteDeleteAsync();
            await dbContext.Albums.Where(album => album.Id == library.Hits.Id).ExecuteDeleteAsync();
            await dbContext.Artists.Where(artist => artist.Id == library.Guest.Id).ExecuteDeleteAsync();
        }

        (await ItemIdsAsync(user)).Should().Equal(library.Ballad.Id);
    }

    [Test]
    public async Task DeleteUser_Always_RemovesTheFavorites()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        LibrarySeed seed = new();
        seed.Favorite(user, library.Ballad);
        await SaveAsync(seed);

        await using WaveMpDbContext dbContext = CreateDbContext();
        await dbContext.Users.Where(candidate => candidate.Id == user.Id).ExecuteDeleteAsync();

        (await dbContext.Favorites.CountAsync(favorite => favorite.UserId == user.Id)).Should().Be(0);
    }

    [Test]
    public async Task Insert_IdsNotMatchingTheType_ViolatesTheCheckConstraint()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        Favorite favorite = NewFavorite(user, FavoriteType.Album, library.FirstAlbum.Id, _start);
        favorite.TrackId = library.Opening.Id;

        await using WaveMpDbContext dbContext = CreateDbContext();
        dbContext.Favorites.Add(favorite);
        Func<Task> save = () => dbContext.SaveChangesAsync();

        (await save.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .Which.ConstraintName.Should().Be("ck_favorites_target");
    }

    private IFavoriteRepository CreateRepository()
    {
        return new FavoriteRepository(CreateDbContext());
    }

    private async Task<IReadOnlyList<Guid>> ItemIdsAsync(User user)
    {
        Page<FavoriteRow> page = await CreateRepository().ListAsync(new FavoriteListQuery(user.Id, 0, 100));
        return [.. page.Items.Select(row => row.ItemId)];
    }

    private static Guid ItemOf(StandardLibrary library, FavoriteType type)
    {
        return type switch
        {
            FavoriteType.Track => library.Ballad.Id,
            FavoriteType.Album => library.SecondAlbum.Id,
            _ => library.Beta.Id,
        };
    }

    private static Favorite NewFavorite(User user, FavoriteType type, Guid itemId, DateTimeOffset createdAt)
    {
        return new Favorite
        {
            Id = Guid.CreateVersion7(createdAt),
            UserId = user.Id,
            Type = type,
            TrackId = type == FavoriteType.Track ? itemId : null,
            AlbumId = type == FavoriteType.Album ? itemId : null,
            ArtistId = type == FavoriteType.Artist ? itemId : null,
            CreatedAt = createdAt,
        };
    }

    private async Task<User> AddUserAsync()
    {
        LibrarySeed seed = new();
        User user = seed.User();
        await SaveAsync(seed);
        return user;
    }
}
