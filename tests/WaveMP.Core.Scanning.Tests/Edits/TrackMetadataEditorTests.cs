using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WaveMP.Core.Metadata.Files;
using WaveMP.Core.Metadata.Tags;
using WaveMP.Core.Scanning.Edits;
using WaveMP.Core.Scanning.Scans;
using WaveMP.Core.Scanning.Writers;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Repositories;
using WaveMP.Testing.Files;

namespace WaveMP.Core.Scanning.Tests.Edits;

[TestFixture]
public class TrackMetadataEditorTests
{
    private const string Directory = "Band/Album";
    private const string AudioPath = "Band/Album/01 - Song.mp3";

    private static readonly DateTimeOffset _now = new(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid _trackId = Guid.CreateVersion7();

    private static readonly TagSnapshot _snapshot = new(
        "Old Title",
        ["Old Artist", "Guest"],
        ["Old Album Artist"],
        "Old Album",
        2001,
        3,
        12,
        1,
        2,
        ["Rock", "Pop"]);

    private TemporaryDirectory _media = null!;
    private string _fullPath = null!;
    private MediaFile _audio = null!;
    private ITrackRepository _tracks = null!;
    private ITrackMetadataRepository _repository = null!;
    private ITagWriter _tagWriter = null!;
    private IFileWriteAccess _writeAccess = null!;
    private IMediaFileLocator _locator = null!;
    private ILibraryScanner _scanner = null!;
    private LibraryWriteLock _writeLock = null!;
    private TrackMetadataEditor _editor = null!;

    [SetUp]
    public void SetUp()
    {
        _media = new TemporaryDirectory();
        _fullPath = _media.Combine(AudioPath);
        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(_fullPath)!);
        File.WriteAllText(_fullPath, "audio");

        _audio = TestFiles.Create(AudioPath, MediaFileKind.Audio, file =>
        {
            file.IsPrimary = true;
            file.IsWritable = true;
            file.Tags = new MediaFileTags
            {
                Title = "Tag Title",
                Artist = "Tag Artist",
                TrackNumber = 3,
            };
        });

        _tracks = Substitute.For<ITrackRepository>();
        _tracks.FindAsync(_trackId, Arg.Any<CancellationToken>()).Returns(Row());
        _repository = Substitute.For<ITrackMetadataRepository>();
        _repository.FindEditTargetAsync(_trackId, Arg.Any<CancellationToken>())
            .Returns(_ => new TrackEditTarget(_trackId, Directory, _audio));
        _tagWriter = Substitute.For<ITagWriter>();
        _tagWriter.ReadSnapshot(_fullPath).Returns(new TagSnapshotResult(TagReadStatus.Read, _snapshot));
        _tagWriter.Write(_fullPath, MediaFileKind.Audio, Arg.Any<TagChanges>()).Returns(TagWriteStatus.Written);
        _writeAccess = Substitute.For<IFileWriteAccess>();
        _writeAccess.IsWritable(_fullPath).Returns(true);
        _locator = Substitute.For<IMediaFileLocator>();
        _locator.TryGetFullPath(AudioPath, out Arg.Any<string>()).Returns(call =>
        {
            call[1] = _fullPath;
            return true;
        });
        _scanner = Substitute.For<ILibraryScanner>();
        _scanner.RefreshFolderAsync(
                Arg.Any<string>(),
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        _writeLock = new LibraryWriteLock();
        TimeProvider timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(_now);

        _editor = new TrackMetadataEditor(
            _tracks,
            _repository,
            _tagWriter,
            _writeAccess,
            _locator,
            _scanner,
            _writeLock,
            timeProvider,
            NullLogger<TrackMetadataEditor>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _writeLock.Dispose();
        _media.Dispose();
    }

    [Test]
    public async Task GetAsync_AudioTrack_ReturnsTheTagsCompletedFromTheLibrary()
    {
        TrackMetadata? metadata = await _editor.GetAsync(_trackId);

        metadata.Should().Be(new TrackMetadata(
            _trackId,
            TrackMetadataStatus.Editable,
            AudioPath,
            "Tag Title",
            "Tag Artist",
            "Library Album",
            "Library Album Artist",
            1999,
            3,
            2,
            "Library Genre"));
        await _repository.DidNotReceiveWithAnyArgs().SetWritableAsync(default, default);
    }

    [Test]
    public async Task GetAsync_UnknownTrack_ReturnsNull()
    {
        (await _editor.GetAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Test]
    public async Task GetAsync_VideoOnly_IsNoAudioWithTheValuesOfTheLibrary()
    {
        _repository.FindEditTargetAsync(_trackId, Arg.Any<CancellationToken>())
            .Returns(new TrackEditTarget(_trackId, Directory, null));

        TrackMetadata? metadata = await _editor.GetAsync(_trackId);

        metadata!.Status.Should().Be(TrackMetadataStatus.NoAudio);
        metadata.AudioFilePath.Should().BeNull();
        (metadata.Title, metadata.Artist).Should().Be(("Library Title", "Library Artist"));
    }

    [Test]
    public async Task GetAsync_StoredAsNotWritableButWritableNow_IsEditableAndStoresTheFlag()
    {
        _audio.IsWritable = false;

        TrackMetadata? metadata = await _editor.GetAsync(_trackId);

        metadata!.Status.Should().Be(TrackMetadataStatus.Editable);
        await _repository.Received(1).SetWritableAsync(_audio.Id, true, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetAsync_MissingFile_IsNotWritable()
    {
        File.Delete(_fullPath);

        (await _editor.GetAsync(_trackId))!.Status.Should().Be(TrackMetadataStatus.NotWritable);
    }

    [Test]
    public async Task UpdateAsync_UnknownTrack_FailsWithTrackNotFound()
    {
        TrackMetadataResult result = await _editor.UpdateAsync(Guid.NewGuid(), TitleChange("New"));

        result.Error.Should().Be(TrackMetadataError.TrackNotFound);
        _tagWriter.DidNotReceiveWithAnyArgs().Write(default!, default, default!);
    }

    [Test]
    public async Task UpdateAsync_VideoOnly_FailsWithNoAudioAndWritesNothing()
    {
        _repository.FindEditTargetAsync(_trackId, Arg.Any<CancellationToken>())
            .Returns(new TrackEditTarget(_trackId, Directory, null));

        TrackMetadataResult result = await _editor.UpdateAsync(_trackId, TitleChange("New"));

        result.Error.Should().Be(TrackMetadataError.NoAudio);
        _tagWriter.ReceivedCalls().Should().BeEmpty();
        await _repository.DidNotReceiveWithAnyArgs().AddTagBackupAsync(default!, default);
    }

    [TestCase(MediaFileIssue.TagsUnreadable)]
    [TestCase(MediaFileIssue.Unreadable)]
    public async Task UpdateAsync_FileWithUnreadableTags_FailsWithNotWritable(MediaFileIssue issue)
    {
        _audio.Issue = issue;

        TrackMetadataResult result = await _editor.UpdateAsync(_trackId, TitleChange("New"));

        result.Error.Should().Be(TrackMetadataError.NotWritable);
        _tagWriter.ReceivedCalls().Should().BeEmpty();
    }

    [Test]
    public async Task UpdateAsync_FileNotWritableNow_FailsAndStoresTheFlag()
    {
        _writeAccess.IsWritable(_fullPath).Returns(false);

        TrackMetadataResult result = await _editor.UpdateAsync(_trackId, TitleChange("New"));

        result.Error.Should().Be(TrackMetadataError.NotWritable);
        await _repository.Received(1).SetWritableAsync(_audio.Id, false, Arg.Any<CancellationToken>());
        _tagWriter.ReceivedCalls().Should().BeEmpty();
        await _scanner.DidNotReceiveWithAnyArgs().RefreshFolderAsync(default!, default!, default);
    }

    [Test]
    public async Task UpdateAsync_NoChanges_ReturnsTheMetadataWithoutBackupWriteOrRefresh()
    {
        TrackMetadataResult result = await _editor.UpdateAsync(_trackId, TagChanges.None);

        result.IsSuccess.Should().BeTrue();
        result.Metadata!.Title.Should().Be("Tag Title");
        _tagWriter.ReceivedCalls().Should().BeEmpty();
        await _repository.DidNotReceiveWithAnyArgs().AddTagBackupAsync(default!, default);
        await _scanner.DidNotReceiveWithAnyArgs().RefreshFolderAsync(default!, default!, default);
    }

    [Test]
    public async Task UpdateAsync_SnapshotFails_FailsWithNotWritableWithoutBackup()
    {
        _tagWriter.ReadSnapshot(_fullPath).Returns(TagSnapshotResult.Failed(TagReadStatus.Unreadable));

        TrackMetadataResult result = await _editor.UpdateAsync(_trackId, TitleChange("New"));

        result.Error.Should().Be(TrackMetadataError.NotWritable);
        await _repository.DidNotReceiveWithAnyArgs().AddTagBackupAsync(default!, default);
        _tagWriter.DidNotReceiveWithAnyArgs().Write(default!, default, default!);
        await _repository.Received(1).SetWritableAsync(_audio.Id, false, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateAsync_Changes_BacksUpWritesAndReadsTheFolderAgainInThisOrder()
    {
        TrackMetadataResult result = await _editor.UpdateAsync(_trackId, TitleChange("New"));

        result.IsSuccess.Should().BeTrue();
        Received.InOrder(() =>
        {
            _repository.AddTagBackupAsync(Arg.Any<TagBackup>(), Arg.Any<CancellationToken>());
            _tagWriter.Write(_fullPath, MediaFileKind.Audio, Arg.Any<TagChanges>());
            _scanner.RefreshFolderAsync(
                Directory,
                Arg.Is<IReadOnlyCollection<string>>(files => files.SequenceEqual(new[] { AudioPath })),
                CancellationToken.None);
        });
    }

    [Test]
    public async Task UpdateAsync_Changes_SavesTheRawTagsAsBackup()
    {
        TagBackup? backup = null;
        await _repository.AddTagBackupAsync(Arg.Do<TagBackup>(value => backup = value), Arg.Any<CancellationToken>());

        await _editor.UpdateAsync(_trackId, TitleChange("New"));

        backup.Should().BeEquivalentTo(new
        {
            MediaFileId = (Guid?)_audio.Id,
            RelativePath = AudioPath,
            Title = "Old Title",
            Performers = new[] { "Old Artist", "Guest" },
            AlbumArtists = new[] { "Old Album Artist" },
            Album = "Old Album",
            Year = 2001,
            TrackNumber = 3,
            TrackCount = 12,
            DiscNumber = 1,
            DiscCount = 2,
            Genres = new[] { "Rock", "Pop" },
            CreatedAt = _now,
        });
    }

    [Test]
    public async Task UpdateAsync_WriteFails_FailsWithNotWritableKeepsTheBackupAndReadsNothingAgain()
    {
        _tagWriter.Write(_fullPath, MediaFileKind.Audio, Arg.Any<TagChanges>()).Returns(TagWriteStatus.NotWritable);

        TrackMetadataResult result = await _editor.UpdateAsync(_trackId, TitleChange("New"));

        result.Error.Should().Be(TrackMetadataError.NotWritable);
        await _repository.Received(1).AddTagBackupAsync(Arg.Any<TagBackup>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SetWritableAsync(_audio.Id, false, Arg.Any<CancellationToken>());
        await _scanner.DidNotReceiveWithAnyArgs().RefreshFolderAsync(default!, default!, default);
    }

    [Test]
    public async Task UpdateAsync_Texts_AreNormalizedBeforeWriting()
    {
        TagChanges? written = null;
        _tagWriter.Write(_fullPath, MediaFileKind.Audio, Arg.Do<TagChanges>(value => written = value))
            .Returns(TagWriteStatus.Written);

        await _editor.UpdateAsync(_trackId, new TagChanges
        {
            Title = new TagChange<string?>("  New \t Title "),
            Album = new TagChange<string?>("   "),
            Genre = new TagChange<string?>(new string('g', 200)),
            Year = new TagChange<int?>(0),
            TrackNumber = new TagChange<int?>(7),
        });

        written.Should().BeEquivalentTo(new TagChanges
        {
            Title = new TagChange<string?>("New Title"),
            Album = new TagChange<string?>(null),
            Genre = new TagChange<string?>(new string('g', 128)),
            Year = new TagChange<int?>(null),
            TrackNumber = new TagChange<int?>(7),
        });
    }

    [Test]
    public void Normalize_LongGenreWithSurrogatePair_DoesNotSplitThePair()
    {
        string genre = new string('g', 127) + "\U0001F3B5";

        TagChanges normalized = TrackMetadataEditor.Normalize(new TagChanges { Genre = new TagChange<string?>(genre) });

        normalized.Genre!.Value.Value.Should().Be(new string('g', 127));
    }

    [Test]
    public void ToBackup_TextsWithNul_RemovesTheNulCharacters()
    {
        TagSnapshot snapshot = _snapshot with
        {
            Title = "Ti\0tle",
            Performers = ["Art\0ist"],
            AlbumArtists = ["\0"],
            Album = "Al\0bum",
            Genres = ["Ro\0ck"],
        };

        TagBackup backup = TrackMetadataEditor.ToBackup(snapshot, _audio, _now);

        (backup.Title, backup.Album).Should().Be(("Title", "Album"));
        backup.Performers.Should().Equal("Artist");
        backup.AlbumArtists.Should().Equal(string.Empty);
        backup.Genres.Should().Equal("Rock");
    }

    [Test]
    public async Task UpdateAsync_ArtistAndAlbumArtist_TakeOverTheTypedSpelling()
    {
        await _editor.UpdateAsync(_trackId, new TagChanges
        {
            Artist = new TagChange<string?>(" ac/dc "),
            AlbumArtist = new TagChange<string?>("AC/DC Band"),
            Title = new TagChange<string?>("Title"),
        });

        await _repository.Received(1).RenameArtistAsync("AC/DC", "ac/dc", CancellationToken.None);
        await _repository.Received(1).RenameArtistAsync("AC/DC BAND", "AC/DC Band", CancellationToken.None);
    }

    [Test]
    public async Task UpdateAsync_RemovedOrUnchangedArtist_RenamesNothing()
    {
        await _editor.UpdateAsync(_trackId, new TagChanges
        {
            Artist = new TagChange<string?>(null),
            Title = new TagChange<string?>("Title"),
        });

        await _repository.DidNotReceiveWithAnyArgs().RenameArtistAsync(default!, default!, default);
    }

    [Test]
    public async Task UpdateAsync_CancelledAfterWriting_StillReadsTheFolderAgain()
    {
        using CancellationTokenSource cancellation = new();
        _tagWriter.Write(_fullPath, MediaFileKind.Audio, Arg.Any<TagChanges>()).Returns(_ =>
        {
            cancellation.Cancel();
            return TagWriteStatus.Written;
        });

        TrackMetadataResult result = await _editor.UpdateAsync(_trackId, TitleChange("New"), cancellation.Token);

        result.IsSuccess.Should().BeTrue();
        await _scanner.Received(1).RefreshFolderAsync(
            Directory,
            Arg.Any<IReadOnlyCollection<string>>(),
            CancellationToken.None);
    }

    [Test]
    public async Task UpdateAsync_WhileTheLockIsHeld_WaitsBeforeWriting()
    {
        Task<TrackMetadataResult> update;
        using (await _writeLock.AcquireAsync())
        {
            update = _editor.UpdateAsync(_trackId, TitleChange("New"));
            await Task.Delay(100);
            _tagWriter.ReceivedCalls().Should().BeEmpty();
        }

        (await update.WaitAsync(TimeSpan.FromSeconds(5))).IsSuccess.Should().BeTrue();
        _tagWriter.Received(1).Write(_fullPath, MediaFileKind.Audio, Arg.Any<TagChanges>());
    }

    private static TagChanges TitleChange(string title)
    {
        return new TagChanges { Title = new TagChange<string?>(title) };
    }

    private static TrackRow Row()
    {
        return new TrackRow(
            _trackId,
            "Library Title",
            Guid.NewGuid(),
            "Library Artist",
            Guid.NewGuid(),
            "Library Album",
            Guid.NewGuid(),
            "Library Album Artist",
            false,
            5,
            2,
            1999,
            "Library Genre",
            TimeSpan.FromMinutes(3),
            true,
            false,
            false,
            _now);
    }
}
