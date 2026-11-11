using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using WaveMP.Data.Entities;
using WaveMP.Data.Repositories;
using WaveMP.Testing.Seeds;

namespace WaveMP.Data.Tests.Repositories;

[TestFixture]
[Category("Integration")]
public class TrackMetadataRepositoryTests : LibraryDatabaseTests
{
    protected override string DatabaseName => "wavemp_tests_library_track_metadata";

    [Test]
    public async Task FindEditTargetAsync_Duplicates_ReturnsThePrimaryAudioFile()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();

        TrackEditTarget? target = await CreateRepository().FindEditTargetAsync(library.Closing.Id);

        target.Should().NotBeNull();
        target!.TrackId.Should().Be(library.Closing.Id);
        target.DirectoryPath.Should().Be(library.Closing.DirectoryPath);
        target.AudioFile.Should().NotBeNull();
        target.AudioFile!.RelativePath.Should().Be("Alpha Band/First Album/02 - Closing.flac");
    }

    [Test]
    public async Task FindEditTargetAsync_VideoOnly_HasNoAudioFile()
    {
        LibrarySeed seed = new();
        Track clip = seed.Track("Clip", hasAudio: false, hasVideo: true);
        seed.File("Clip.mp4", MediaFileKind.Video, clip);
        await SaveAsync(seed);

        TrackEditTarget? target = await CreateRepository().FindEditTargetAsync(clip.Id);

        target.Should().NotBeNull();
        target!.AudioFile.Should().BeNull();
    }

    [Test]
    public async Task FindEditTargetAsync_UnknownTrack_ReturnsNull()
    {
        await SeedStandardLibraryAsync();

        TrackEditTarget? target = await CreateRepository().FindEditTargetAsync(Guid.NewGuid());

        target.Should().BeNull();
    }

    [Test]
    public async Task AddTagBackupAsync_Backup_IsSavedWithListsAndNumbers()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        MediaFile file = await FindFileAsync("Alpha Band/First Album/01 - Opening.mp3");
        TagBackup backup = NewBackup(file);

        await CreateRepository().AddTagBackupAsync(backup);

        TagBackup stored = await CreateDbContext().TagBackups.AsNoTracking().SingleAsync();
        stored.Should().BeEquivalentTo(backup, options => options.Excluding(item => item.MediaFile));
        stored.MediaFileId.Should().Be(file.Id);
        library.Opening.Id.Should().Be(file.TrackId!.Value);
    }

    [Test]
    public async Task AddTagBackupAsync_FileDeletedLater_KeepsTheBackupWithoutReference()
    {
        await SeedStandardLibraryAsync();
        MediaFile file = await FindFileAsync("Alpha Band/First Album/01 - Opening.mp3");
        await CreateRepository().AddTagBackupAsync(NewBackup(file));

        await CreateDbContext().MediaFiles.Where(item => item.Id == file.Id).ExecuteDeleteAsync();

        TagBackup stored = await CreateDbContext().TagBackups.AsNoTracking().SingleAsync();
        stored.MediaFileId.Should().BeNull();
        stored.RelativePath.Should().Be(file.RelativePath);
        stored.Title.Should().Be("Original Title");
    }

    [Test]
    public async Task SetWritableAsync_File_ChangesTheFlag()
    {
        await SeedStandardLibraryAsync();
        MediaFile file = await FindFileAsync("Alpha Band/First Album/01 - Opening.mp3");
        file.IsWritable.Should().BeTrue();

        await CreateRepository().SetWritableAsync(file.Id, false);

        (await FindFileAsync(file.RelativePath)).IsWritable.Should().BeFalse();
    }

    [Test]
    public async Task RenameArtistAsync_OtherSpelling_ChangesOnlyTheName()
    {
        StandardLibrary library = await SeedStandardLibraryAsync();
        ITrackMetadataRepository repository = CreateRepository();

        int changed = await repository.RenameArtistAsync("ALPHA BAND", "ALPHA band");
        int unchanged = await repository.RenameArtistAsync("ALPHA BAND", "ALPHA band");
        int missing = await repository.RenameArtistAsync("NOBODY", "Nobody");

        (changed, unchanged, missing).Should().Be((1, 0, 0));
        Artist artist = await CreateDbContext().Artists.AsNoTracking().SingleAsync(item => item.Id == library.Alpha.Id);
        artist.Name.Should().Be("ALPHA band");
        artist.NormalizedName.Should().Be("ALPHA BAND");
    }

    private ITrackMetadataRepository CreateRepository()
    {
        return new TrackMetadataRepository(CreateDbContext());
    }

    private Task<MediaFile> FindFileAsync(string relativePath)
    {
        return CreateDbContext().MediaFiles.AsNoTracking().SingleAsync(file => file.RelativePath == relativePath);
    }

    private static TagBackup NewBackup(MediaFile file)
    {
        return new TagBackup
        {
            Id = Guid.CreateVersion7(),
            MediaFileId = file.Id,
            RelativePath = file.RelativePath,
            Title = "Original Title",
            Performers = ["First Artist", "Second Artist"],
            AlbumArtists = ["Album Artist"],
            Album = "Original Album",
            Year = 2001,
            TrackNumber = 3,
            TrackCount = 12,
            DiscNumber = 1,
            DiscCount = 2,
            Genres = ["Rock", "Pop"],
            CreatedAt = new DateTimeOffset(2026, 11, 11, 12, 0, 0, TimeSpan.Zero),
        };
    }
}
