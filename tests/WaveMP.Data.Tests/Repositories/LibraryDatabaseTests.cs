using Microsoft.EntityFrameworkCore;
using WaveMP.Data.Entities;
using WaveMP.Testing.Databases;
using WaveMP.Testing.Seeds;

namespace WaveMP.Data.Tests.Repositories;

/// <summary>
/// Base of the tests of the library repositories against a real PostgreSQL database with all migrations (including
/// pg_trgm). Every fixture uses its own database; every test starts with an empty library.
/// </summary>
public abstract class LibraryDatabaseTests
{
    private TestDatabase _database = null!;
    private readonly List<WaveMpDbContext> _contexts = [];

    /// <summary>
    /// Name of the database of the fixture.
    /// </summary>
    protected abstract string DatabaseName { get; }

    [OneTimeSetUp]
    public async Task CreateDatabaseAsync()
    {
        _database = await TestDatabase.CreateMigratedAsync(DatabaseName);
    }

    [OneTimeTearDown]
    public async Task DropDatabaseAsync()
    {
        await _database.DisposeAsync();
    }

    [SetUp]
    public async Task ClearLibraryAsync()
    {
        await using WaveMpDbContext dbContext = _database.CreateDbContext();
        await dbContext.Database.ExecuteSqlRawAsync(
            "TRUNCATE covers, media_files, tracks, albums, artists RESTART IDENTITY CASCADE");
    }

    [TearDown]
    public async Task DisposeContextsAsync()
    {
        foreach (WaveMpDbContext context in _contexts)
        {
            await context.DisposeAsync();
        }

        _contexts.Clear();
    }

    /// <summary>
    /// A context for the repository under test, disposed after the test.
    /// </summary>
    protected WaveMpDbContext CreateDbContext()
    {
        WaveMpDbContext context = _database.CreateDbContext();
        _contexts.Add(context);
        return context;
    }

    protected async Task SaveAsync(LibrarySeed seed)
    {
        await using WaveMpDbContext dbContext = _database.CreateDbContext();
        await seed.SaveAsync(dbContext);
    }

    /// <summary>
    /// A small library with albums of two artists, a compilation, tracks without album and covers:
    /// <list type="bullet">
    /// <item>First Album (Alpha Band, 2001): Opening (Rock, 3:00), Closing (rock, 4:00, video), Duet (Guest Star,
    /// Rock, 2:00); album cover</item>
    /// <item>Second Album (Beta Singer, 2010): Ballad (Pop, 5:00) with an embedded cover</item>
    /// <item>Hits Collection (compilation, 2005): Hit One (Alpha Band, Pop), Hit Two (Beta Singer, Pop), no cover
    /// </item>
    /// <item>without album: Clip (Beta Singer, video only), Loose Song (Guest Star)</item>
    /// </list>
    /// </summary>
    protected async Task<StandardLibrary> SeedStandardLibraryAsync()
    {
        LibrarySeed seed = new();
        DateTimeOffset first = LibrarySeed.DefaultTime;
        DateTimeOffset second = first.AddDays(1);
        DateTimeOffset third = first.AddDays(2);

        Artist alpha = seed.Artist("Alpha Band", first);
        Artist beta = seed.Artist("Beta Singer", second);
        Artist guest = seed.Artist("Guest Star", first);

        Album firstAlbum = seed.Album("First Album", alpha, year: 2001, firstSeenAt: first);
        Album secondAlbum = seed.Album("Second Album", beta, year: 2010, firstSeenAt: second);
        Album hits = seed.Album("Hits Collection", isCompilation: true, year: 2005, firstSeenAt: third);

        var three = TimeSpan.FromMinutes(3);
        Track opening = seed.Track("Opening", alpha, firstAlbum, 1, 1, 2001, "Rock", three, firstSeenAt: first);
        Track closing = seed.Track(
            "Closing",
            alpha,
            firstAlbum,
            2,
            1,
            2001,
            "rock",
            TimeSpan.FromMinutes(4),
            hasVideo: true,
            firstSeenAt: first);
        Track duet = seed.Track(
            "Duet", guest, firstAlbum, 3, 1, 2001, "Rock", TimeSpan.FromMinutes(2), firstSeenAt: first);
        Track ballad = seed.Track(
            "Ballad", beta, secondAlbum, 1, 1, 2010, "Pop", TimeSpan.FromMinutes(5), firstSeenAt: second);
        Track hitOne = seed.Track("Hit One", alpha, hits, 1, null, 2005, "Pop", three, firstSeenAt: third);
        Track hitTwo = seed.Track("Hit Two", beta, hits, 2, null, 2005, "Pop", three, firstSeenAt: third);
        Track clip = seed.Track("Clip", beta, hasAudio: false, hasVideo: true, firstSeenAt: third);
        Track looseSong = seed.Track("Loose Song", guest, duration: TimeSpan.FromMinutes(1), firstSeenAt: third);

        const string Folder = "Alpha Band/First Album/";
        seed.File(Folder + "01 - Opening.mp3", MediaFileKind.Audio, opening);
        seed.File(Folder + "02 - Closing.flac", MediaFileKind.Audio, closing, size: 9000, isLossless: true);
        seed.File(Folder + "02 - Closing.mp3", MediaFileKind.Audio, closing, isPrimary: false, size: 3000);
        seed.File(Folder + "02 - Closing.mkv", MediaFileKind.Video, closing, size: 20000);
        seed.File(Folder + "02 - Closing.jpg", MediaFileKind.Image, closing, isPrimary: false, size: 500);
        MediaFile albumPicture = seed.File(Folder + "cover.jpg", MediaFileKind.Image, isPrimary: false);
        seed.AlbumCover(firstAlbum, albumPicture);
        MediaFile balladFile = seed.File(
            "Beta Singer/Second Album/01 - Ballad.m4a", MediaFileKind.Audio, ballad, hasEmbeddedPicture: true);
        seed.TrackCover(ballad, balladFile, CoverSource.Embedded, mimeType: "image/png");

        await SaveAsync(seed);
        return new StandardLibrary(
            alpha,
            beta,
            guest,
            firstAlbum,
            secondAlbum,
            hits,
            opening,
            closing,
            duet,
            ballad,
            hitOne,
            hitTwo,
            clip,
            looseSong);
    }

    protected sealed record StandardLibrary(
        Artist Alpha,
        Artist Beta,
        Artist Guest,
        Album FirstAlbum,
        Album SecondAlbum,
        Album Hits,
        Track Opening,
        Track Closing,
        Track Duet,
        Track Ballad,
        Track HitOne,
        Track HitTwo,
        Track Clip,
        Track LooseSong);
}
