using WaveMP.Core.Playlists.Covers;
using WaveMP.Core.Playlists.Histories;
using WaveMP.Core.Playlists.Playlists;
using WaveMP.Data.Repositories;
using WaveMP.Service.Dtos;

namespace WaveMP.Service.Mappings;

public static class PlaylistMappingExtensions
{
    public static PlaylistDto ToDto(this PlaylistItem item)
    {
        return new PlaylistDto
        {
            Id = item.Playlist.Id,
            Name = item.Playlist.Name,
            TrackCount = item.Playlist.TrackCount,
            DurationMilliseconds = (long)Math.Round(item.Playlist.DurationMilliseconds),
            Cover = item.Cover.ToDto(),
            CreatedAt = item.Playlist.CreatedAt,
            UpdatedAt = item.Playlist.UpdatedAt,
        };
    }

    public static PlaylistCoverDto ToDto(this PlaylistCover cover)
    {
        return new PlaylistCoverDto
        {
            Kind = cover.Kind.ToDto(),
            Version = cover.Version,
            TrackIds = cover.TrackIds,
        };
    }

    public static PlaylistCoverKindDto ToDto(this PlaylistCoverKind kind)
    {
        return kind switch
        {
            PlaylistCoverKind.Custom => PlaylistCoverKindDto.Custom,
            PlaylistCoverKind.Tracks => PlaylistCoverKindDto.Tracks,
            PlaylistCoverKind.None => PlaylistCoverKindDto.None,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown cover kind."),
        };
    }

    public static PlaylistEntryDto ToDto(this PlaylistEntryItem item)
    {
        return new PlaylistEntryDto
        {
            Id = item.Entry.Id,
            Position = item.Entry.Position,
            AddedAt = item.Entry.AddedAt,
            Track = item.Track.ToDto(),
        };
    }

    public static RecentlyPlayedTrackDto ToDto(this RecentlyPlayedItem item)
    {
        return new RecentlyPlayedTrackDto
        {
            LastPlayedAt = item.LastPlayedAt,
            Track = item.Track.ToDto(),
        };
    }

    public static PlaylistSortField ToEntity(this PlaylistSortDto sort)
    {
        return sort switch
        {
            PlaylistSortDto.Name => PlaylistSortField.Name,
            PlaylistSortDto.CreatedAt => PlaylistSortField.CreatedAt,
            PlaylistSortDto.UpdatedAt => PlaylistSortField.UpdatedAt,
            _ => throw new ArgumentOutOfRangeException(nameof(sort), sort, "Unknown sort key."),
        };
    }
}
