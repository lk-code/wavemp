using AwesomeAssertions;
using WaveMP.Core.Library.Queries;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Library.Tests.Queries;

[TestFixture]
public class LibraryQueryDefaultsTests
{
    [TestCase(null, false, TrackSortField.AlbumArtist)]
    [TestCase(null, true, TrackSortField.Relevance)]
    [TestCase(TrackSortField.Title, true, TrackSortField.Title)]
    [TestCase(TrackSortField.Relevance, false, TrackSortField.AlbumArtist)]
    [TestCase(TrackSortField.Relevance, true, TrackSortField.Relevance)]
    public void TrackSort_Always_UsesTheRequestedKeyOrTheDefault(
        TrackSortField? sort,
        bool hasSearch,
        TrackSortField expected)
    {
        LibraryQueryDefaults.TrackSort(sort, hasSearch).Should().Be(expected);
    }

    [Test]
    public void TrackSort_WithFallback_UsesTheFallbackWithoutSearch()
    {
        LibraryQueryDefaults.TrackSort(null, false, TrackSortField.Album).Should().Be(TrackSortField.Album);
        LibraryQueryDefaults.TrackSort(TrackSortField.Relevance, false, TrackSortField.Album)
            .Should().Be(TrackSortField.Album);
        LibraryQueryDefaults.TrackSort(null, true, TrackSortField.Album).Should().Be(TrackSortField.Relevance);
    }

    [TestCase(null, false, AlbumSortField.Artist)]
    [TestCase(null, true, AlbumSortField.Relevance)]
    [TestCase(AlbumSortField.Relevance, false, AlbumSortField.Artist)]
    [TestCase(AlbumSortField.Year, false, AlbumSortField.Year)]
    public void AlbumSort_Always_UsesTheRequestedKeyOrTheDefault(
        AlbumSortField? sort,
        bool hasSearch,
        AlbumSortField expected)
    {
        LibraryQueryDefaults.AlbumSort(sort, hasSearch).Should().Be(expected);
    }

    [TestCase(null, false, ArtistSortField.Name)]
    [TestCase(null, true, ArtistSortField.Relevance)]
    [TestCase(ArtistSortField.Relevance, false, ArtistSortField.Name)]
    [TestCase(ArtistSortField.FirstSeenAt, true, ArtistSortField.FirstSeenAt)]
    public void ArtistSort_Always_UsesTheRequestedKeyOrTheDefault(
        ArtistSortField? sort,
        bool hasSearch,
        ArtistSortField expected)
    {
        LibraryQueryDefaults.ArtistSort(sort, hasSearch).Should().Be(expected);
    }

    [Test]
    public void GenreAndFileSort_WithoutKey_UseNameAndKind()
    {
        LibraryQueryDefaults.GenreSort(null).Should().Be(GenreSortField.Name);
        LibraryQueryDefaults.GenreSort(GenreSortField.TrackCount).Should().Be(GenreSortField.TrackCount);
        LibraryQueryDefaults.FileSort(null).Should().Be(MediaFileSortField.Kind);
        LibraryQueryDefaults.FileSort(MediaFileSortField.Size).Should().Be(MediaFileSortField.Size);
    }

    [TestCase(TrackSortField.AlbumArtist, SortDirection.Asc)]
    [TestCase(TrackSortField.Title, SortDirection.Asc)]
    [TestCase(TrackSortField.Duration, SortDirection.Asc)]
    [TestCase(TrackSortField.FirstSeenAt, SortDirection.Desc)]
    [TestCase(TrackSortField.Relevance, SortDirection.Desc)]
    public void Direction_OfTracksWithoutDirection_DescendsForRecentAndRelevant(
        TrackSortField sort,
        SortDirection expected)
    {
        LibraryQueryDefaults.Direction(sort, null).Should().Be(expected);
        LibraryQueryDefaults.Direction(sort, SortDirection.Asc).Should().Be(SortDirection.Asc);
    }

    [Test]
    public void Direction_OfTheOtherLists_DescendsForRecentRelevantAndCounts()
    {
        LibraryQueryDefaults.Direction(AlbumSortField.Artist, null).Should().Be(SortDirection.Asc);
        LibraryQueryDefaults.Direction(AlbumSortField.FirstSeenAt, null).Should().Be(SortDirection.Desc);
        LibraryQueryDefaults.Direction(AlbumSortField.Relevance, null).Should().Be(SortDirection.Desc);
        LibraryQueryDefaults.Direction(ArtistSortField.Name, null).Should().Be(SortDirection.Asc);
        LibraryQueryDefaults.Direction(ArtistSortField.Relevance, null).Should().Be(SortDirection.Desc);
        LibraryQueryDefaults.Direction(GenreSortField.Name, null).Should().Be(SortDirection.Asc);
        LibraryQueryDefaults.Direction(GenreSortField.TrackCount, null).Should().Be(SortDirection.Desc);
        LibraryQueryDefaults.Direction(GenreSortField.TrackCount, SortDirection.Asc).Should().Be(SortDirection.Asc);
        LibraryQueryDefaults.FileDirection(null).Should().Be(SortDirection.Asc);
        LibraryQueryDefaults.YearDirection(null).Should().Be(SortDirection.Desc);
        LibraryQueryDefaults.YearDirection(SortDirection.Asc).Should().Be(SortDirection.Asc);
    }
}
