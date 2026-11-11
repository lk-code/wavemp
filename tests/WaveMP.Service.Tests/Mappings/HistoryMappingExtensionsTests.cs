using AwesomeAssertions;
using WaveMP.Core.Streaming.Histories;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Service.Dtos;
using WaveMP.Service.Mappings;

namespace WaveMP.Service.Tests.Mappings;

[TestFixture]
public class HistoryMappingExtensionsTests
{
    [Test]
    public void ToDto_Always_CarriesTheEntryAndItsTrack()
    {
        TrackRow track = new(
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
        PlayHistoryEntry entry = new()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TrackId = track.Id,
            PlayedAt = DateTimeOffset.UnixEpoch.AddDays(1),
        };

        PlayHistoryEntryDto dto = new PlayHistoryItem(entry, track).ToDto();

        dto.Id.Should().Be(entry.Id);
        dto.PlayedAt.Should().Be(entry.PlayedAt);
        dto.Track.Should().BeEquivalentTo(track.ToDto());
    }
}
