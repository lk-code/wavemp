using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WaveMP.Data.Entities;
using WaveMP.Data.Repositories;
using WaveMP.Testing.Seeds;

namespace WaveMP.Data.Tests.Repositories;

[TestFixture]
[Category("Integration")]
public class TrackRatingRepositoryTests : LibraryDatabaseTests
{
    private static readonly DateTimeOffset _start = new(2026, 11, 1, 20, 0, 0, TimeSpan.Zero);

    protected override string DatabaseName => "wavemp_tests_track_ratings";

    [Test]
    public async Task SetAsync_NewRating_AddsIt()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();

        bool set = await CreateRepository().SetAsync(user.Id, library.Ballad.Id, 4, _start);

        set.Should().BeTrue();
        (await RatingsAsync(user)).Should().ContainSingle().Which.Should().BeEquivalentTo(
            new { TrackId = library.Ballad.Id, Rating = 4, UpdatedAt = _start });
    }

    [Test]
    public async Task SetAsync_ChangedRating_ReplacesItWithItsTime()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        await CreateRepository().SetAsync(user.Id, library.Ballad.Id, 4, _start);

        await CreateRepository().SetAsync(user.Id, library.Ballad.Id, 2, _start.AddHours(1));

        (await RatingsAsync(user)).Should().ContainSingle().Which.Should().BeEquivalentTo(
            new { Rating = 2, UpdatedAt = _start.AddHours(1) });
    }

    [Test]
    public async Task SetAsync_UnchangedRating_KeepsItsTime()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        await CreateRepository().SetAsync(user.Id, library.Ballad.Id, 5, _start);

        bool set = await CreateRepository().SetAsync(user.Id, library.Ballad.Id, 5, _start.AddHours(1));

        set.Should().BeTrue();
        (await RatingsAsync(user)).Should().ContainSingle().Which.UpdatedAt.Should().Be(_start);
    }

    [Test]
    public async Task SetAsync_SameTrackOfAnotherUser_KeepsBothRatings()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        User other = await AddUserAsync();

        await CreateRepository().SetAsync(user.Id, library.Ballad.Id, 1, _start);
        await CreateRepository().SetAsync(other.Id, library.Ballad.Id, 5, _start);

        (await RatingsAsync(user)).Should().ContainSingle().Which.Rating.Should().Be(1);
        (await RatingsAsync(other)).Should().ContainSingle().Which.Rating.Should().Be(5);
    }

    [Test]
    public async Task SetAsync_UnknownTrack_ReturnsFalse()
    {
        await SeedStandardLibraryAsync();
        User user = await AddUserAsync();

        bool set = await CreateRepository().SetAsync(user.Id, Guid.NewGuid(), 3, _start);

        set.Should().BeFalse();
        (await RatingsAsync(user)).Should().BeEmpty();
    }

    [Test]
    public async Task RemoveAsync_Rating_RemovesOnlyTheRatingOfTheUser()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        User other = await AddUserAsync();
        LibrarySeed seed = new();
        seed.Rating(user, library.Ballad, 3);
        seed.Rating(user, library.Opening, 4);
        seed.Rating(other, library.Ballad, 5);
        await SaveAsync(seed);

        bool removed = await CreateRepository().RemoveAsync(user.Id, library.Ballad.Id);
        bool again = await CreateRepository().RemoveAsync(user.Id, library.Ballad.Id);

        removed.Should().BeTrue();
        again.Should().BeTrue();
        (await RatingsAsync(user)).Select(rating => rating.TrackId).Should().Equal(library.Opening.Id);
        (await RatingsAsync(other)).Should().ContainSingle();
    }

    [Test]
    public async Task RemoveAsync_UnknownTrack_ReturnsFalse()
    {
        await SeedStandardLibraryAsync();
        User user = await AddUserAsync();

        (await CreateRepository().RemoveAsync(user.Id, Guid.NewGuid())).Should().BeFalse();
    }

    [Test]
    public async Task DeleteTrack_WithExecuteDelete_RemovesItsRatings()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();
        LibrarySeed seed = new();
        seed.Rating(user, library.LooseSong, 3);
        seed.Rating(user, library.Ballad, 4);
        await SaveAsync(seed);

        await using (WaveMpDbContext dbContext = CreateDbContext())
        {
            await dbContext.Tracks.Where(track => track.Id == library.LooseSong.Id).ExecuteDeleteAsync();
        }

        (await RatingsAsync(user)).Select(rating => rating.TrackId).Should().Equal(library.Ballad.Id);
    }

    [TestCase(0)]
    [TestCase(6)]
    public async Task Insert_RatingOutOfRange_ViolatesTheCheckConstraint(int value)
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        User user = await AddUserAsync();

        await using WaveMpDbContext dbContext = CreateDbContext();
        dbContext.TrackRatings.Add(new TrackRating
        {
            UserId = user.Id,
            TrackId = library.Ballad.Id,
            Rating = value,
            UpdatedAt = _start,
        });
        Func<Task> save = () => dbContext.SaveChangesAsync();

        (await save.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .Which.ConstraintName.Should().Be("ck_track_ratings_rating");
    }

    private ITrackRatingRepository CreateRepository()
    {
        return new TrackRatingRepository(CreateDbContext());
    }

    private async Task<List<TrackRating>> RatingsAsync(User user)
    {
        await using WaveMpDbContext dbContext = CreateDbContext();
        return await dbContext.TrackRatings.AsNoTracking()
            .Where(rating => rating.UserId == user.Id)
            .OrderBy(rating => rating.TrackId)
            .ToListAsync();
    }

    private async Task<User> AddUserAsync()
    {
        LibrarySeed seed = new();
        User user = seed.User();
        await SaveAsync(seed);
        return user;
    }
}
