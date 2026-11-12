using WaveMP.Data;
using WaveMP.Data.Entities;

namespace WaveMP.Testing.Seeds;

/// <summary>
/// Builds library data (artists, albums, tracks, files and covers) for tests of the data access, with sensible values
/// for everything a test does not care about. Nothing is written until <see cref="SaveAsync"/>.
/// </summary>
public sealed class LibrarySeed
{
    /// <summary>
    /// Time of the entities that set none of their own.
    /// </summary>
    public static readonly DateTimeOffset DefaultTime = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly List<object> _entities = [];
    private int _counter;

    public Artist Artist(string name, DateTimeOffset? firstSeenAt = null)
    {
        Artist artist = new()
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            FirstSeenAt = firstSeenAt ?? DefaultTime,
        };
        _entities.Add(artist);
        return artist;
    }

    public Album Album(
        string title,
        Artist? albumArtist = null,
        bool isCompilation = false,
        int? year = null,
        DateTimeOffset? firstSeenAt = null)
    {
        Album album = new()
        {
            Id = Guid.CreateVersion7(),
            Title = title,
            NormalizedTitle = title.ToUpperInvariant(),
            AlbumKey = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
            AlbumArtistId = albumArtist?.Id,
            IsCompilation = isCompilation,
            Year = year,
            FirstSeenAt = firstSeenAt ?? DefaultTime,
            UpdatedAt = firstSeenAt ?? DefaultTime,
        };
        _entities.Add(album);
        return album;
    }

    public Track Track(
        string title,
        Artist? artist = null,
        Album? album = null,
        int? number = null,
        int? disc = null,
        int? year = null,
        string? genre = null,
        TimeSpan? duration = null,
        bool hasAudio = true,
        bool hasVideo = false,
        DateTimeOffset? firstSeenAt = null)
    {
        string baseName = $"{++_counter:D4} {title}";
        Track track = new()
        {
            Id = Guid.CreateVersion7(),
            DirectoryPath = album?.Title ?? string.Empty,
            BaseName = baseName,
            NormalizedBaseName = baseName.ToUpperInvariant(),
            Title = title,
            ArtistId = artist?.Id,
            AlbumId = album?.Id,
            TrackNumber = number,
            DiscNumber = disc,
            Year = year,
            Genre = genre,
            Duration = duration,
            HasAudio = hasAudio,
            HasVideo = hasVideo,
            MetadataSource = TrackMetadataSource.Tags,
            FirstSeenAt = firstSeenAt ?? DefaultTime,
            UpdatedAt = firstSeenAt ?? DefaultTime,
        };
        _entities.Add(track);
        return track;
    }

    /// <summary>
    /// A file of the media folder; <paramref name="track"/> is <c>null</c> for pictures without a track, such as the
    /// cover of an album.
    /// </summary>
    public MediaFile File(
        string relativePath,
        MediaFileKind kind,
        Track? track = null,
        bool isPrimary = true,
        long size = 1000,
        bool isLossless = false,
        bool hasEmbeddedPicture = false,
        MediaFileIssue? issue = null)
    {
        int slash = relativePath.LastIndexOf('/');
        string fileName = slash < 0 ? relativePath : relativePath[(slash + 1)..];
        int dot = fileName.LastIndexOf('.');
        string baseName = dot < 0 ? fileName : fileName[..dot];
        MediaFile file = new()
        {
            Id = Guid.CreateVersion7(),
            RelativePath = relativePath,
            DirectoryPath = slash < 0 ? string.Empty : relativePath[..slash],
            FileName = fileName,
            BaseName = baseName,
            NormalizedBaseName = baseName.ToUpperInvariant(),
            Extension = dot < 0 ? string.Empty : fileName[(dot + 1)..].ToLowerInvariant(),
            Kind = kind,
            Size = size,
            ModifiedAt = DefaultTime,
            TrackId = track?.Id,
            IsPrimary = isPrimary,
            IsLossless = isLossless,
            HasEmbeddedPicture = hasEmbeddedPicture,
            IsWritable = kind == MediaFileKind.Audio,
            Issue = issue,
            MetadataVersion = 1,
            FirstSeenAt = DefaultTime,
            ScannedAt = DefaultTime,
        };
        _entities.Add(file);
        return file;
    }

    public Cover AlbumCover(Album album, MediaFile file, bool isPrimary = true, string? mimeType = "image/jpeg")
    {
        return Add(new Cover
        {
            Id = Guid.CreateVersion7(),
            AlbumId = album.Id,
            Source = CoverSource.File,
            MediaFileId = file.Id,
            MimeType = mimeType,
            IsPrimary = isPrimary,
        });
    }

    public Cover TrackCover(
        Track track,
        MediaFile file,
        CoverSource source = CoverSource.File,
        bool isPrimary = true,
        string? mimeType = "image/jpeg")
    {
        return Add(new Cover
        {
            Id = Guid.CreateVersion7(),
            TrackId = track.Id,
            Source = source,
            MediaFileId = file.Id,
            MimeType = mimeType,
            IsPrimary = isPrimary,
        });
    }

    /// <summary>
    /// A profile with a unique name; the tests share the users of a database, so names never repeat.
    /// </summary>
    public User User(string? name = null)
    {
        string unique = name ?? "Listener " + Guid.NewGuid().ToString("N")[..12];
        User user = new()
        {
            Id = Guid.CreateVersion7(),
            Name = unique,
            NormalizedName = unique.ToUpperInvariant(),
            Initials = "L",
            Color = "#336699",
            Role = UserRole.User,
            IsBuiltIn = false,
            CreatedAt = DefaultTime,
        };
        _entities.Add(user);
        return user;
    }

    public Favorite Favorite(User user, Track track, DateTimeOffset? createdAt = null)
    {
        return AddFavorite(user, FavoriteType.Track, createdAt, trackId: track.Id);
    }

    public Favorite Favorite(User user, Album album, DateTimeOffset? createdAt = null)
    {
        return AddFavorite(user, FavoriteType.Album, createdAt, albumId: album.Id);
    }

    public Favorite Favorite(User user, Artist artist, DateTimeOffset? createdAt = null)
    {
        return AddFavorite(user, FavoriteType.Artist, createdAt, artistId: artist.Id);
    }

    public TrackRating Rating(User user, Track track, int rating, DateTimeOffset? updatedAt = null)
    {
        TrackRating trackRating = new()
        {
            UserId = user.Id,
            TrackId = track.Id,
            Rating = rating,
            UpdatedAt = updatedAt ?? DefaultTime,
        };
        _entities.Add(trackRating);
        return trackRating;
    }

    /// <summary>
    /// A playlist of the user with the tracks in the given order.
    /// </summary>
    public Playlist Playlist(User user, string name, params Track[] tracks)
    {
        Playlist playlist = new()
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            CreatedAt = DefaultTime,
            UpdatedAt = DefaultTime,
        };
        _entities.Add(playlist);
        _entities.AddRange(tracks.Select((track, index) => new PlaylistEntry
        {
            Id = Guid.CreateVersion7(),
            PlaylistId = playlist.Id,
            TrackId = track.Id,
            Position = index,
            AddedAt = DefaultTime,
        }));
        return playlist;
    }

    /// <summary>
    /// Adds everything built so far to the database.
    /// </summary>
    public async Task SaveAsync(WaveMpDbContext dbContext)
    {
        dbContext.AddRange(_entities);
        await dbContext.SaveChangesAsync();
        _entities.Clear();
    }

    private Cover Add(Cover cover)
    {
        _entities.Add(cover);
        return cover;
    }

    private Favorite AddFavorite(
        User user,
        FavoriteType type,
        DateTimeOffset? createdAt,
        Guid? trackId = null,
        Guid? albumId = null,
        Guid? artistId = null)
    {
        Favorite favorite = new()
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            Type = type,
            TrackId = trackId,
            AlbumId = albumId,
            ArtistId = artistId,
            CreatedAt = createdAt ?? DefaultTime,
        };
        _entities.Add(favorite);
        return favorite;
    }
}
