using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WaveMP.Core.Jobs.Extensions;
using WaveMP.Core.Metadata.Extensions;
using WaveMP.Core.Metadata.Tags;
using WaveMP.Core.Scanning.Edits;
using WaveMP.Core.Scanning.Extensions;
using WaveMP.Core.Scanning.Plans;
using WaveMP.Core.Scanning.Scans;
using WaveMP.Core.Settings.Extensions;
using WaveMP.Data;
using WaveMP.Data.Entities;
using WaveMP.Data.Extensions;
using WaveMP.Testing.Databases;
using WaveMP.Testing.Files;
using WaveMP.Testing.Media;

namespace WaveMP.Core.Scanning.Tests.Edits;

/// <summary>
/// Edits synthetic media created with ffmpeg in a library scanned into a real PostgreSQL database.
/// </summary>
[TestFixture]
[Category("Integration")]
public class TrackMetadataEditorDatabaseTests
{
    private const string DatabaseName = "wavemp_tests_metadata_editor";

    private TestDatabase _database = null!;
    private TemporaryDirectory _media = null!;
    private ServiceProvider _services = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _database = await TestDatabase.CreateMigratedAsync(DatabaseName);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _database.DisposeAsync();
    }

    [SetUp]
    public async Task SetUp()
    {
        await using WaveMpDbContext dbContext = _database.CreateDbContext();
        await dbContext.Database.ExecuteSqlRawAsync(
            "TRUNCATE tag_backups, covers, media_files, tracks, albums, artists RESTART IDENTITY CASCADE");
        _media = new TemporaryDirectory();
        _services = CreateServices(_media.Path);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _services.DisposeAsync();
        _media.Dispose();
    }

    [Test]
    public async Task UpdateAsync_AllFields_WritesTheFileBacksUpTheTagsAndUpdatesTheLibrary()
    {
        TestTags album = new() { Artist = "Band", Album = "Album", Date = "2001", Genre = "Rock" };
        await AudioAsync("Band/Album/01 - One.mp3", album with { Title = "One", Track = "1/2" });
        await AudioAsync("Band/Album/02 - Two.mp3", album with { Title = "Two", Track = "2/2" });
        await ScanAsync();
        Guid trackId = await TrackIdAsync("One");

        TrackMetadataResult result = await UpdateAsync(trackId, new TagChanges
        {
            Title = new TagChange<string?>("Uno"),
            Artist = new TagChange<string?>("Solo Artist"),
            Album = new TagChange<string?>("Solo Album"),
            AlbumArtist = new TagChange<string?>("Solo Artist"),
            Year = new TagChange<int?>(2020),
            TrackNumber = new TagChange<int?>(5),
            DiscNumber = new TagChange<int?>(1),
            Genre = new TagChange<string?>("Jazz"),
        });

        result.IsSuccess.Should().BeTrue();
        result.Metadata.Should().Be(new TrackMetadata(
            trackId,
            TrackMetadataStatus.Editable,
            "Band/Album/01 - One.mp3",
            "Uno",
            "Solo Artist",
            "Solo Album",
            "Solo Artist",
            2020,
            5,
            1,
            "Jazz"));

        using (var file = TagLib.File.Create(_media.Combine("Band/Album/01 - One.mp3")))
        {
            (file.Tag.Title, file.Tag.FirstPerformer, file.Tag.Album, file.Tag.Year, file.Tag.Track)
                .Should().Be(("Uno", "Solo Artist", "Solo Album", 2020u, 5u));
            file.Tag.TrackCount.Should().Be(2u);
        }

        await using WaveMpDbContext db = _database.CreateDbContext();
        TagBackup backup = await db.TagBackups.SingleAsync();
        backup.Should().BeEquivalentTo(new
        {
            RelativePath = "Band/Album/01 - One.mp3",
            Title = "One",
            Performers = new[] { "Band" },
            Album = "Album",
            Year = 2001,
            TrackNumber = 1,
            TrackCount = 2,
            Genres = new[] { "Rock" },
        });
        Track track = await db.Tracks
            .Include(item => item.Artist)
            .Include(item => item.Album).ThenInclude(item => item!.AlbumArtist)
            .SingleAsync(item => item.Id == trackId);
        (track.Title, track.Artist!.Name, track.Album!.Title, track.Album.AlbumArtist!.Name)
            .Should().Be(("Uno", "Solo Artist", "Solo Album", "Solo Artist"));
        (track.Year, track.TrackNumber, track.DiscNumber, track.Genre).Should().Be((2020, 5, 1, "Jazz"));
        (await db.Albums.Select(item => item.Title).OrderBy(title => title).ToListAsync())
            .Should().Equal("Album", "Solo Album");
    }

    [Test]
    public async Task UpdateAsync_OnlyTrackOfAnAlbum_RemovesTheOldAlbumAndArtist()
    {
        await AudioAsync("Solo/Record/01 - Only.mp3", new TestTags { Title = "Only", Artist = "Solo", Album = "Record" });
        await ScanAsync();
        Guid trackId = await TrackIdAsync("Only");

        TrackMetadataResult result = await UpdateAsync(trackId, new TagChanges
        {
            Artist = new TagChange<string?>("Soloist"),
            Album = new TagChange<string?>("Renamed"),
        });

        result.IsSuccess.Should().BeTrue();
        await using WaveMpDbContext db = _database.CreateDbContext();
        (await db.Albums.Select(album => album.Title).ToListAsync()).Should().Equal("Renamed");
        (await db.Artists.Select(artist => artist.Name).ToListAsync()).Should().Equal("Soloist");
    }

    [Test]
    public async Task UpdateAsync_ArtistInOtherCase_TakesOverTheSpellingForAllTracks()
    {
        await AudioAsync("Band/01 - One.mp3", new TestTags { Title = "One", Artist = "the band" });
        await AudioAsync("Band/02 - Two.mp3", new TestTags { Title = "Two", Artist = "the band" });
        await ScanAsync();
        Guid artistId;
        await using (WaveMpDbContext before = _database.CreateDbContext())
        {
            artistId = (await before.Artists.SingleAsync()).Id;
        }

        await UpdateAsync(await TrackIdAsync("One"), new TagChanges { Artist = new TagChange<string?>("The Band") });

        await using WaveMpDbContext db = _database.CreateDbContext();
        Artist artist = await db.Artists.SingleAsync();
        (artist.Id, artist.Name).Should().Be((artistId, "The Band"));
        (await db.Tracks.CountAsync(track => track.ArtistId == artistId)).Should().Be(2);
    }

    [Test]
    public async Task UpdateAsync_NullTitle_FallsBackToTheFileName()
    {
        await AudioAsync("Band/01 - Named.mp3", new TestTags { Title = "Tagged" });
        await ScanAsync();
        Guid trackId = await TrackIdAsync("Tagged");

        TrackMetadataResult result = await UpdateAsync(trackId, new TagChanges { Title = new TagChange<string?>(null) });

        result.Metadata!.Title.Should().Be("Named");
        await using WaveMpDbContext db = _database.CreateDbContext();
        Track track = await db.Tracks.SingleAsync(item => item.Id == trackId);
        (track.Title, track.MetadataSource).Should().Be(("Named", TrackMetadataSource.Path));
    }

    [Test]
    public async Task UpdateAsync_ReadOnlyFile_WritesNothing()
    {
        await AudioAsync("Locked.mp3", new TestTags { Title = "Locked" });
        await ScanAsync();
        File.SetAttributes(_media.Combine("Locked.mp3"), FileAttributes.ReadOnly);
        Guid trackId = await TrackIdAsync("Locked");

        TrackMetadataResult result = await UpdateAsync(trackId, new TagChanges { Title = new TagChange<string?>("X") });

        result.Error.Should().Be(TrackMetadataError.NotWritable);
        await using WaveMpDbContext db = _database.CreateDbContext();
        (await db.TagBackups.CountAsync()).Should().Be(0);
        (await db.MediaFiles.SingleAsync()).IsWritable.Should().BeFalse();
    }

    private async Task<TrackMetadataResult> UpdateAsync(Guid trackId, TagChanges changes)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ITrackMetadataEditor>().UpdateAsync(trackId, changes);
    }

    private async Task<Guid> TrackIdAsync(string title)
    {
        await using WaveMpDbContext db = _database.CreateDbContext();
        return await db.Tracks.Where(track => track.Title == title).Select(track => track.Id).SingleAsync();
    }

    private async Task ScanAsync()
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ILibraryScanner>().ScanAsync(ScanScope.Full, Guid.NewGuid());
    }

    private Task AudioAsync(string relativePath, TestTags tags)
    {
        return TestMediaGenerator.CreateAudioAsync(_media.Combine(relativePath), AudioFormat.Mp3, tags);
    }

    private static ServiceProvider CreateServices(string mediaPath)
    {
        Dictionary<string, string?> settings = TestDatabaseSettings.ToConfiguration(DatabaseName);
        settings["MEDIA_PATH"] = mediaPath;
        settings["Metadata:FfprobePath"] = FfmpegTool.FfprobePath;
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        ServiceCollection services = new();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddWaveMpData();
        services.AddWaveMpSettings();
        services.AddWaveMpMetadata();
        services.AddWaveMpJobs();
        services.AddWaveMpScanning();
        return services.BuildServiceProvider();
    }
}
