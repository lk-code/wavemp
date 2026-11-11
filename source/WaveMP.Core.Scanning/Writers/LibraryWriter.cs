using WaveMP.Core.Metadata.Readers;
using WaveMP.Core.Metadata.Resolutions;
using WaveMP.Core.Metadata.Tags;
using WaveMP.Core.Metadata.Texts;
using WaveMP.Core.Scanning.Discoveries;
using WaveMP.Core.Scanning.Groupings;
using WaveMP.Core.Scanning.Plans;
using WaveMP.Core.Scanning.Processors;
using WaveMP.Data.Entities;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Scanning.Writers;

/// <summary>
/// Writes the result of the scan of a folder into the library: files, tracks with their primary files and covers,
/// artists and albums. All changes of a folder are saved together. Album covers are written after all folders.
/// </summary>
public sealed class LibraryWriter(ILibraryScanRepository library)
{
    private const int GenreMaxLength = 128;

    /// <summary>
    /// Updates the files of a changed folder with the metadata read for its new and changed files, and its tracks.
    /// </summary>
    public async Task<FolderWriteResult> WriteFolderAsync(
        DirectoryPlan plan,
        IReadOnlyDictionary<string, MediaFileMetadata> metadata,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        StoredFolder stored = await library.LoadFolderAsync(plan.RelativePath, cancellationToken);
        var storedFiles = stored.Files.ToDictionary(
            file => file.RelativePath,
            StringComparer.Ordinal);

        List<MediaFile> files = [];
        foreach (DiscoveredFile discovered in plan.Files)
        {
            storedFiles.Remove(discovered.RelativePath, out MediaFile? file);
            if (metadata.TryGetValue(discovered.RelativePath, out MediaFileMetadata? read))
            {
                if (file is null)
                {
                    file = CreateFile(discovered, plan.RelativePath, now);
                    library.Add(file);
                }

                Apply(file, discovered, read, now);
            }

            if (file is not null)
            {
                files.Add(file);
            }
        }

        foreach (MediaFile removed in storedFiles.Values)
        {
            library.Remove(removed);
        }

        FolderWriteResult result = await WriteTracksAsync(
            plan.RelativePath,
            FolderProcessor.Process(plan.RelativePath, files),
            stored.Tracks,
            now,
            cancellationToken);
        await library.SaveChangesAsync(cancellationToken);
        return result;
    }

    /// <summary>
    /// Replaces the covers of all albums with the pictures of their folders named after the cover names.
    /// </summary>
    public async Task UpdateAlbumCoversAsync(
        IReadOnlyList<string> coverNames,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AlbumFolder> folders = await library.GetAlbumFoldersAsync(cancellationToken);
        IReadOnlyList<AlbumCoverImage> images = await library.GetAlbumCoverImagesAsync(
            [.. coverNames.Select(FileGrouper.NormalizeBaseName)],
            cancellationToken);
        IReadOnlyDictionary<Guid, IReadOnlyList<AlbumCoverImage>> desired =
            AlbumCoverCandidates.Select(folders, images, coverNames);
        ILookup<Guid, Cover> existing = (await library.LoadAlbumCoversAsync(cancellationToken))
            .ToLookup(cover => cover.AlbumId!.Value);

        HashSet<Guid> albums = [.. desired.Keys, .. existing.Select(group => group.Key)];
        foreach (Guid albumId in albums)
        {
            IReadOnlyList<AlbumCoverImage> candidates = desired.GetValueOrDefault(albumId, []);
            SyncCovers(
                [.. existing[albumId]],
                [.. candidates.Select((image, index) => (
                    image.MediaFileId,
                    CoverSource.File,
                    image.Width,
                    image.Height,
                    FolderProcessor.MimeTypeOf(image.Extension),
                    index == 0))],
                cover => cover.AlbumId = albumId);
        }

        await library.SaveChangesAsync(cancellationToken);
    }

    private async Task<FolderWriteResult> WriteTracksAsync(
        string directoryPath,
        ProcessedFolder folder,
        IReadOnlyList<Track> storedTracks,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var tracks = storedTracks.ToDictionary(
            track => track.NormalizedBaseName,
            StringComparer.Ordinal);
        EntityCache cache = new();
        int added = 0;
        int updated = 0;

        foreach (ProcessedTrack processed in folder.Tracks)
        {
            Guid? artistId = await GetArtistIdAsync(processed.Metadata.Artist, cache, now, cancellationToken);
            Guid? albumId = await GetAlbumIdAsync(processed.Album, cache, now, cancellationToken);

            if (!tracks.Remove(processed.Group.NormalizedBaseName, out Track? track))
            {
                track = new Track
                {
                    Id = Guid.CreateVersion7(now),
                    DirectoryPath = directoryPath,
                    BaseName = processed.Group.BaseName,
                    NormalizedBaseName = processed.Group.NormalizedBaseName,
                    Title = processed.Metadata.Title,
                    HasAudio = false,
                    HasVideo = false,
                    MetadataSource = processed.Metadata.TitleSource,
                    FirstSeenAt = now,
                    UpdatedAt = now,
                };
                library.Add(track);
                added++;
                Update(track, processed, artistId, albumId);
            }
            else if (Update(track, processed, artistId, albumId))
            {
                track.UpdatedAt = now;
                updated++;
            }

            foreach (MediaFile file in processed.Group.Files)
            {
                file.TrackId = track.Id;
                file.IsPrimary = file == processed.PrimaryAudio || file == processed.PrimaryVideo;
            }

            Track owner = track;
            SyncCovers(
                [.. track.Covers],
                [.. processed.Covers.Select((cover, index) => (
                    cover.File.Id,
                    cover.Source,
                    cover.Width,
                    cover.Height,
                    cover.MimeType,
                    index == 0))],
                cover => cover.TrackId = owner.Id);
        }

        foreach (MediaFile file in folder.PictureGroups.SelectMany(group => group.Files))
        {
            file.TrackId = null;
            file.IsPrimary = false;
        }

        foreach (Track removed in tracks.Values)
        {
            library.Remove(removed);
        }

        return new FolderWriteResult(added, updated, tracks.Count);
    }

    /// <summary>
    /// Applies the processed values to the track. Returns whether anything changed.
    /// </summary>
    private static bool Update(Track track, ProcessedTrack processed, Guid? artistId, Guid? albumId)
    {
        ResolvedTrackMetadata metadata = processed.Metadata;
        string? genre = metadata.Genre is { Length: > GenreMaxLength } longGenre
            ? longGenre[..GenreMaxLength]
            : metadata.Genre;
        bool hasAudio = processed.PrimaryAudio is not null;
        bool hasVideo = processed.PrimaryVideo is not null;

        bool changed = track.BaseName != processed.Group.BaseName
            || track.Title != metadata.Title
            || track.MetadataSource != metadata.TitleSource
            || track.ArtistId != artistId
            || track.AlbumId != albumId
            || track.TrackNumber != metadata.TrackNumber
            || track.DiscNumber != metadata.DiscNumber
            || track.Year != metadata.Year
            || track.Genre != genre
            || track.Duration != processed.Duration
            || track.HasAudio != hasAudio
            || track.HasVideo != hasVideo;

        track.BaseName = processed.Group.BaseName;
        track.Title = metadata.Title;
        track.MetadataSource = metadata.TitleSource;
        track.ArtistId = artistId;
        track.AlbumId = albumId;
        track.TrackNumber = metadata.TrackNumber;
        track.DiscNumber = metadata.DiscNumber;
        track.Year = metadata.Year;
        track.Genre = genre;
        track.Duration = processed.Duration;
        track.HasAudio = hasAudio;
        track.HasVideo = hasVideo;
        return changed;
    }

    /// <summary>
    /// Makes the covers of an album or track match the wanted ones (file and source), keeping the ids of covers that
    /// stay.
    /// </summary>
    private void SyncCovers(
        List<Cover> existing,
        IReadOnlyList<(Guid MediaFileId, CoverSource Source, int? Width, int? Height, string? MimeType, bool IsPrimary)> wanted,
        Action<Cover> assignOwner)
    {
        foreach ((Guid mediaFileId, CoverSource source, int? width, int? height, string? mimeType, bool isPrimary) in wanted)
        {
            Cover? cover = existing.Find(item => item.MediaFileId == mediaFileId && item.Source == source);
            if (cover is null)
            {
                cover = new Cover
                {
                    Id = Guid.CreateVersion7(),
                    Source = source,
                    MediaFileId = mediaFileId,
                    IsPrimary = isPrimary,
                };
                assignOwner(cover);
                library.Add(cover);
            }
            else
            {
                existing.Remove(cover);
            }

            cover.Width = width;
            cover.Height = height;
            cover.MimeType = mimeType;
            cover.IsPrimary = isPrimary;
        }

        foreach (Cover removed in existing)
        {
            library.Remove(removed);
        }
    }

    private async Task<Guid?> GetArtistIdAsync(
        string? name,
        EntityCache cache,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (MetadataText.Normalize(name) is not { } displayName || MetadataText.ToKey(displayName) is not { } key)
        {
            return null;
        }

        if (cache.Artists.TryGetValue(key, out Guid id))
        {
            return id;
        }

        Artist? artist = await library.FindArtistAsync(key, cancellationToken);
        if (artist is null)
        {
            artist = new Artist
            {
                Id = Guid.CreateVersion7(now),
                Name = displayName,
                NormalizedName = key,
                FirstSeenAt = now,
            };
            library.Add(artist);
        }

        cache.Artists[key] = artist.Id;
        return artist.Id;
    }

    private async Task<Guid?> GetAlbumIdAsync(
        AlbumAssignment? assignment,
        EntityCache cache,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (assignment is null)
        {
            return null;
        }

        if (cache.Albums.TryGetValue(assignment.Key, out Guid id))
        {
            return id;
        }

        Album? album = await library.FindAlbumAsync(assignment.Key, cancellationToken);
        if (album is null)
        {
            album = new Album
            {
                Id = Guid.CreateVersion7(now),
                Title = assignment.Title,
                NormalizedTitle = MetadataText.ToKey(assignment.Title)!,
                AlbumKey = assignment.Key,
                AlbumArtistId = await GetArtistIdAsync(assignment.AlbumArtist, cache, now, cancellationToken),
                IsCompilation = assignment.IsCompilation,
                Year = assignment.Year,
                FirstSeenAt = now,
                UpdatedAt = now,
            };
            library.Add(album);
        }
        else if (album.Year is null && assignment.Year is not null)
        {
            album.Year = assignment.Year;
            album.UpdatedAt = now;
        }

        cache.Albums[assignment.Key] = album.Id;
        return album.Id;
    }

    private static MediaFile CreateFile(DiscoveredFile discovered, string directoryPath, DateTimeOffset now)
    {
        string baseName = MediaKindClassifier.BaseNameOf(discovered.FileName);
        return new MediaFile
        {
            Id = Guid.CreateVersion7(now),
            RelativePath = discovered.RelativePath,
            DirectoryPath = directoryPath,
            FileName = discovered.FileName,
            BaseName = baseName,
            NormalizedBaseName = FileGrouper.NormalizeBaseName(baseName),
            Extension = MediaKindClassifier.ExtensionOf(discovered.FileName),
            Kind = discovered.Kind,
            Size = discovered.Size,
            ModifiedAt = discovered.ModifiedAt,
            IsPrimary = false,
            IsLossless = false,
            HasEmbeddedPicture = false,
            IsWritable = false,
            MetadataVersion = ScannerVersions.Metadata,
            FirstSeenAt = now,
            ScannedAt = now,
        };
    }

    private static void Apply(MediaFile file, DiscoveredFile discovered, MediaFileMetadata metadata, DateTimeOffset now)
    {
        file.Kind = discovered.Kind;
        file.Size = discovered.Size;
        file.ModifiedAt = discovered.ModifiedAt;
        file.Format = metadata.Format;
        file.Codec = metadata.Codec;
        file.Bitrate = metadata.Bitrate;
        file.SampleRate = metadata.SampleRate;
        file.BitDepth = metadata.BitDepth;
        file.Channels = metadata.Channels;
        file.IsLossless = metadata.IsLossless;
        file.Width = metadata.Width;
        file.Height = metadata.Height;
        file.Duration = metadata.Duration;
        file.HasEmbeddedPicture = metadata.HasEmbeddedPicture;
        file.EmbeddedPictureWidth = metadata.EmbeddedPictureWidth;
        file.EmbeddedPictureHeight = metadata.EmbeddedPictureHeight;
        file.EmbeddedPictureMimeType = metadata.EmbeddedPictureMimeType;
        file.IsWritable = metadata.IsWritable;
        file.Issue = metadata.Issue;
        file.Tags = ToEntity(metadata.Tags);
        file.MetadataVersion = ScannerVersions.Metadata;
        file.ScannedAt = now;
    }

    private static MediaFileTags ToEntity(MediaTags tags)
    {
        return new MediaFileTags
        {
            Title = tags.Title,
            TitleSort = tags.TitleSort,
            Artist = tags.Artist,
            Artists = tags.Artists,
            ArtistSort = tags.ArtistSort,
            AlbumArtist = tags.AlbumArtist,
            AlbumArtistSort = tags.AlbumArtistSort,
            Album = tags.Album,
            AlbumSort = tags.AlbumSort,
            TrackNumber = tags.TrackNumber,
            TrackCount = tags.TrackCount,
            DiscNumber = tags.DiscNumber,
            DiscCount = tags.DiscCount,
            Year = tags.Year,
            Genre = tags.Genre is { Length: > GenreMaxLength } genre ? genre[..GenreMaxLength] : tags.Genre,
            Genres = tags.Genres,
            Composer = tags.Composer,
            ReplayGainTrackGain = tags.ReplayGainTrackGain,
            ReplayGainTrackPeak = tags.ReplayGainTrackPeak,
            ReplayGainAlbumGain = tags.ReplayGainAlbumGain,
            ReplayGainAlbumPeak = tags.ReplayGainAlbumPeak,
            MusicBrainzTrackId = Truncate(tags.MusicBrainzTrackId, 64),
            MusicBrainzReleaseId = Truncate(tags.MusicBrainzReleaseId, 64),
        };
    }

    private static string? Truncate(string? value, int length)
    {
        return value is not null && value.Length > length ? value[..length] : value;
    }

    /// <summary>
    /// Artists and albums found or created while writing one folder.
    /// </summary>
    private sealed class EntityCache
    {
        public Dictionary<string, Guid> Artists { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, Guid> Albums { get; } = new(StringComparer.Ordinal);
    }
}
