using WaveMP.Core.Streaming.Histories;
using WaveMP.Service.Dtos;

namespace WaveMP.Service.Mappings;

public static class HistoryMappingExtensions
{
    public static PlayHistoryEntryDto ToDto(this PlayHistoryItem item)
    {
        return new PlayHistoryEntryDto
        {
            Id = item.Entry.Id,
            PlayedAt = item.Entry.PlayedAt,
            Track = item.Track.ToDto(),
        };
    }
}
