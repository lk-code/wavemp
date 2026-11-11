using AwesomeAssertions;
using WaveMP.Core.Caching.Covers;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;

namespace WaveMP.Core.Caching.Tests.Covers;

[TestFixture]
public class CoverCacheKeyTests
{
    private static readonly Guid _fileId = Guid.Parse("019a6f2c-1111-7abc-8def-0123456789ab");
    private static readonly DateTimeOffset _modifiedAt = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public void PrefixOf_SameVersion_IsStableAndNamesTheFileAndSource()
    {
        string prefix = CoverCacheKey.PrefixOf(_fileId, CoverSource.File, _modifiedAt, 1234);

        prefix.Should().Be(CoverCacheKey.PrefixOf(_fileId, CoverSource.File, _modifiedAt, 1234));
        prefix.Should().MatchRegex("^019a6f2c11117abc8def0123456789ab-f-[0-9a-f]{16}$");
        CoverCacheKey.PrefixOf(_fileId, CoverSource.Embedded, _modifiedAt, 1234).Should().Contain("-e-");
    }

    [Test]
    public void PrefixOf_ChangedFile_Changes()
    {
        string prefix = CoverCacheKey.PrefixOf(_fileId, CoverSource.File, _modifiedAt, 1234);

        string[] others =
        [
            CoverCacheKey.PrefixOf(Guid.NewGuid(), CoverSource.File, _modifiedAt, 1234),
            CoverCacheKey.PrefixOf(_fileId, CoverSource.Embedded, _modifiedAt, 1234),
            CoverCacheKey.PrefixOf(_fileId, CoverSource.File, _modifiedAt.AddTicks(10), 1234),
            CoverCacheKey.PrefixOf(_fileId, CoverSource.File, _modifiedAt, 1235),
        ];

        others.Should().NotContain(prefix).And.OnlyHaveUniqueItems();
    }

    [Test]
    public void PrefixOf_CandidateAndSourceOfTheSameFile_AreTheSame()
    {
        CoverCandidateRow candidate = new(
            Guid.NewGuid(),
            _fileId,
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            null,
            true,
            CoverSource.Embedded,
            "Album/Song.mp3",
            null,
            99,
            _modifiedAt);

        CoverCacheKey.PrefixOf(candidate).Should().Be(
            CoverCacheKey.PrefixOf(new CoverSourceRow(_fileId, CoverSource.Embedded, 99, _modifiedAt)));
    }

    [Test]
    public void FileNameAndETags_OfAPrefix_UseTheLastCharactersOfTheIdAsFolder()
    {
        string prefix = CoverCacheKey.PrefixOf(_fileId, CoverSource.File, _modifiedAt, 1234);

        CoverCacheKey.FileNameOf(prefix, 96).Should().Be($"ab/{prefix}-96.webp");
        CoverCacheKey.ETagOf(prefix, 300).Should().Be($"{prefix}-300");
        CoverCacheKey.OriginalETagOf(prefix).Should().Be($"{prefix}-original");
    }

    [Test]
    public void TryReadSourcePrefix_NameOfAScaledPicture_ReturnsItsPrefix()
    {
        string prefix = CoverCacheKey.PrefixOf(_fileId, CoverSource.File, _modifiedAt, 1234);

        bool read = CoverCacheKey.TryReadSourcePrefix(CoverCacheKey.FileNameOf(prefix, 1000), out string result);

        read.Should().BeTrue();
        result.Should().Be(prefix);
    }

    [TestCase("ab/picture.webp")]
    [TestCase("other.txt")]
    [TestCase("cd/019a6f2c11117abc8def0123456789ab-f-0123456789abcdef-96.webp")]
    [TestCase("ab/019a6f2c11117abc8def0123456789ab-x-0123456789abcdef-96.webp")]
    [TestCase("ab/019a6f2c11117abc8def0123456789ab-f-0123456789abcdef-96.png")]
    [TestCase("ab/019A6F2C11117ABC8DEF0123456789AB-f-0123456789abcdef-96.webp")]
    public void TryReadSourcePrefix_OtherNames_ReturnsFalse(string fileName)
    {
        CoverCacheKey.TryReadSourcePrefix(fileName, out _).Should().BeFalse();
    }
}
