using AwesomeAssertions;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Validations;
using WaveMP.Data.Entities;

namespace WaveMP.Core.Settings.Tests.Validations;

[TestFixture]
public class LibrarySettingsValidatorTests
{
    [Test]
    public void Validate_Defaults_HasNoErrors()
    {
        LibrarySettings defaults = new();

        IReadOnlyList<SettingsValidationError> errors =
            LibrarySettingsValidator.Validate(LibrarySettingsValidator.Normalize(defaults));

        errors.Should().BeEmpty();
        LibrarySettingsValidator.Normalize(defaults).Should().BeEquivalentTo(defaults);
    }

    [Test]
    public void Normalize_Lists_TrimsLowersRemovesDotsEmptyAndDuplicateEntries()
    {
        LibrarySettings settings = new()
        {
            AudioExtensions = [" .MP3", "flac", "", "  ", "Mp3", "..ogg"],
            AlbumCoverNames = [" Cover ", "FOLDER", "cover"],
        };

        LibrarySettings normalized = LibrarySettingsValidator.Normalize(settings);

        normalized.AudioExtensions.Should().Equal("mp3", "flac", "ogg");
        normalized.AlbumCoverNames.Should().Equal("cover", "folder");
    }

    [TestCase(14, true)]
    [TestCase(15, false)]
    [TestCase(1440, false)]
    [TestCase(1441, true)]
    [TestCase(-1, true)]
    public void Validate_ScanInterval_MustBeWithinRange(int minutes, bool invalid)
    {
        LibrarySettings settings = new() { ScanIntervalMinutes = minutes };

        IReadOnlyList<SettingsValidationError> errors = LibrarySettingsValidator.Validate(settings);

        if (invalid)
        {
            errors.Should().Equal(
                new SettingsValidationError(LibrarySettingsField.ScanIntervalMinutes, SettingsErrorReason.OutOfRange));
        }
        else
        {
            errors.Should().BeEmpty();
        }
    }

    [TestCase("mp-3")]
    [TestCase("abcdefghijk")]
    [TestCase("mp3!")]
    [TestCase("ä")]
    public void Validate_ExtensionWithInvalidForm_IsInvalid(string extension)
    {
        LibrarySettings settings = LibrarySettingsValidator.Normalize(new LibrarySettings
        {
            ImageExtensions = ["jpg", extension],
        });

        IReadOnlyList<SettingsValidationError> errors = LibrarySettingsValidator.Validate(settings);

        errors.Should().Equal(
            new SettingsValidationError(LibrarySettingsField.ImageExtensions, SettingsErrorReason.Invalid));
    }

    [Test]
    public void Validate_ExtensionInTwoLists_ReportsTheLaterList()
    {
        LibrarySettings settings = new() { AudioExtensions = ["mp3", "mp4"], VideoExtensions = ["mp4", "mkv"] };

        IReadOnlyList<SettingsValidationError> errors = LibrarySettingsValidator.Validate(settings);

        errors.Should().Equal(
            new SettingsValidationError(LibrarySettingsField.VideoExtensions, SettingsErrorReason.Overlap));
    }

    [Test]
    public void Validate_ImageExtensionThatIsAlsoVideo_IsAnOverlap()
    {
        LibrarySettings settings = new() { ImageExtensions = ["jpg", "mkv"] };

        IReadOnlyList<SettingsValidationError> errors = LibrarySettingsValidator.Validate(settings);

        errors.Should().Equal(
            new SettingsValidationError(LibrarySettingsField.ImageExtensions, SettingsErrorReason.Overlap));
    }

    [Test]
    public void Validate_TooManyEntries_IsReported()
    {
        LibrarySettings settings = new()
        {
            AudioExtensions = [.. Enumerable.Range(0, 51).Select(index => $"a{index}")],
            AlbumCoverNames = [.. Enumerable.Range(0, 51).Select(index => $"cover{index}")],
        };

        IReadOnlyList<SettingsValidationError> errors = LibrarySettingsValidator.Validate(settings);

        errors.Should().BeEquivalentTo(
        [
            new SettingsValidationError(LibrarySettingsField.AudioExtensions, SettingsErrorReason.TooMany),
            new SettingsValidationError(LibrarySettingsField.AlbumCoverNames, SettingsErrorReason.TooMany),
        ]);
    }

    [Test]
    public void Validate_NoAudioAndNoVideoExtensions_IsEmpty()
    {
        LibrarySettings settings = new() { AudioExtensions = [], VideoExtensions = [], ImageExtensions = [] };

        IReadOnlyList<SettingsValidationError> errors = LibrarySettingsValidator.Validate(settings);

        errors.Should().BeEquivalentTo(
        [
            new SettingsValidationError(LibrarySettingsField.AudioExtensions, SettingsErrorReason.Empty),
            new SettingsValidationError(LibrarySettingsField.VideoExtensions, SettingsErrorReason.Empty),
        ]);
    }

    [Test]
    public void Validate_OnlyVideoExtensionsAndNoCoverNames_IsValid()
    {
        LibrarySettings settings = new() { AudioExtensions = [], ImageExtensions = [], AlbumCoverNames = [] };

        LibrarySettingsValidator.Validate(settings).Should().BeEmpty();
    }

    [TestCase(".cover")]
    [TestCase("covers/front")]
    [TestCase("covers\\front")]
    [TestCase("abcdefghijabcdefghijabcdefghijabcdefghijabcdefghijk")]
    public void Validate_InvalidCoverName_IsInvalid(string name)
    {
        LibrarySettings settings = new() { AlbumCoverNames = ["cover", name] };

        IReadOnlyList<SettingsValidationError> errors = LibrarySettingsValidator.Validate(settings);

        errors.Should().Equal(
            new SettingsValidationError(LibrarySettingsField.AlbumCoverNames, SettingsErrorReason.Invalid));
    }

    [Test]
    public void CoverPriority_ByDefault_IsAlbumFirst()
    {
        new LibrarySettings().CoverPriority.Should().Be(CoverPriority.AlbumFirst);
    }

    [Test]
    public void Normalize_CoverPriority_KeepsTheValue()
    {
        LibrarySettings settings = new() { CoverPriority = CoverPriority.TrackFirst };

        LibrarySettingsValidator.Normalize(settings).CoverPriority.Should().Be(CoverPriority.TrackFirst);
    }

    [Test]
    public void Validate_UndefinedCoverPriority_IsInvalid()
    {
        LibrarySettings settings = new() { CoverPriority = (CoverPriority)7 };

        IReadOnlyList<SettingsValidationError> errors = LibrarySettingsValidator.Validate(settings);

        errors.Should().Equal(
            new SettingsValidationError(LibrarySettingsField.CoverPriority, SettingsErrorReason.Invalid));
    }
}
