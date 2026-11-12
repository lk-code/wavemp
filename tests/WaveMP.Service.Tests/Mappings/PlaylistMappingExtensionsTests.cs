using AwesomeAssertions;
using WaveMP.Core.Playlists.Covers;
using WaveMP.Core.Playlists.Histories;
using WaveMP.Core.Playlists.Playlists;
using WaveMP.Data.Projections;
using WaveMP.Data.Repositories;
using WaveMP.Service.Dtos;
using WaveMP.Service.Mappings;

namespace WaveMP.Service.Tests.Mappings;

[TestFixture]
public class PlaylistMappingExtensionsTests
{
    private static readonly DateTimeOffset _created = new(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);

    [Test]
    public void ToDto_PlaylistWithTrackCovers_CarriesItsCountsAndTheTracksOfTheCover()
    {
        Guid[] trackIds = [Guid.NewGuid(), Guid.NewGuid()];
        PlaylistRow row = new(Guid.NewGuid(), "Mix", 3, 180_499.6, null, _created, _created.AddDays(1));

        PlaylistDto dto = new PlaylistItem(row, new PlaylistCover(PlaylistCoverKind.Tracks, null, trackIds)).ToDto();

        dto.Should().BeEquivalentTo(new
        {
            row.Id,
            Name = "Mix",
            TrackCount = 3,
            DurationMilliseconds = 180_500L,
            CreatedAt = _created,
            UpdatedAt = _created.AddDays(1),
        });
        dto.Cover.Kind.Should().Be(PlaylistCoverKindDto.Tracks);
        dto.Cover.Version.Should().BeNull();
        dto.Cover.TrackIds.Should().Equal(trackIds);
    }

    [TestCase(PlaylistCoverKind.Custom, PlaylistCoverKindDto.Custom)]
    [TestCase(PlaylistCoverKind.Tracks, PlaylistCoverKindDto.Tracks)]
    [TestCase(PlaylistCoverKind.None, PlaylistCoverKindDto.None)]
    public void ToDto_CoverKind_MapsEveryKind(PlaylistCoverKind kind, PlaylistCoverKindDto expected)
    {
        kind.ToDto().Should().Be(expected);
    }

    [Test]
    public void ToDto_CustomCover_CarriesItsVersion()
    {
        PlaylistCoverDto dto = new PlaylistCover(PlaylistCoverKind.Custom, "abc", []).ToDto();

        dto.Version.Should().Be("abc");
        dto.TrackIds.Should().BeEmpty();
    }

    [Test]
    public void ToDto_Entry_CarriesItsPositionAndTrack()
    {
        TrackRow track = Track();
        PlaylistEntryRow entry = new(Guid.NewGuid(), track.Id, 4, _created);

        PlaylistEntryDto dto = new PlaylistEntryItem(entry, track).ToDto();

        dto.Id.Should().Be(entry.Id);
        dto.Position.Should().Be(4);
        dto.AddedAt.Should().Be(_created);
        dto.Track.Should().BeEquivalentTo(track.ToDto());
    }

    [Test]
    public void ToDto_RecentlyPlayed_CarriesTheLastPlayAndTheTrack()
    {
        TrackRow track = Track();

        RecentlyPlayedTrackDto dto = new RecentlyPlayedItem(track, _created).ToDto();

        dto.LastPlayedAt.Should().Be(_created);
        dto.Track.Should().BeEquivalentTo(track.ToDto());
    }

    [TestCase(PlaylistSortDto.Name, PlaylistSortField.Name)]
    [TestCase(PlaylistSortDto.CreatedAt, PlaylistSortField.CreatedAt)]
    [TestCase(PlaylistSortDto.UpdatedAt, PlaylistSortField.UpdatedAt)]
    public void ToEntity_Sort_MapsEveryKey(PlaylistSortDto sort, PlaylistSortField expected)
    {
        sort.ToEntity().Should().Be(expected);
    }

    private static TrackRow Track()
    {
        return new TrackRow(
            Guid.NewGuid(),
            "Song",
            null,
            "Band",
            null,
            null,
            null,
            null,
            false,
            1,
            null,
            2001,
            null,
            TimeSpan.FromSeconds(3),
            true,
            false,
            false,
            DateTimeOffset.UnixEpoch);
    }
}
