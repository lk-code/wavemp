using Microsoft.Extensions.Logging;
using WaveMP.Core.Metadata.Files;
using WaveMP.Core.Metadata.Tags;
using WaveMP.Core.Metadata.Texts;
using WaveMP.Core.Scanning.Scans;
using WaveMP.Core.Scanning.Writers;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Scanning.Edits;

/// <summary>
/// Writes metadata into the primary audio file of a track: checks the file, saves its current tags as a backup,
/// writes the changes while holding the <see cref="LibraryWriteLock"/> (so no scan reads the file meanwhile) and then
/// reads the folder of the track again with the logic of the scan. A spelling of an artist that differs only in case
/// or spaces is taken over, so a corrected name shows everywhere.
/// </summary>
internal sealed class TrackMetadataEditor(
    ITrackRepository tracks,
    ITrackMetadataRepository repository,
    ITagWriter tagWriter,
    IFileWriteAccess writeAccess,
    IMediaFileLocator locator,
    ILibraryScanner scanner,
    LibraryWriteLock writeLock,
    TimeProvider timeProvider,
    ILogger<TrackMetadataEditor> logger) : ITrackMetadataEditor
{
    private const int GenreMaxLength = 128;

    public async Task<TrackMetadata?> GetAsync(Guid trackId, CancellationToken cancellationToken = default)
    {
        TrackRow? row = await tracks.FindAsync(trackId, cancellationToken: cancellationToken);
        TrackEditTarget? target = row is null
            ? null
            : await repository.FindEditTargetAsync(trackId, cancellationToken);
        if (row is null || target is null)
        {
            return null;
        }

        (TrackMetadataStatus status, _) = await EvaluateAsync(target.AudioFile, cancellationToken);
        MediaFileTags? tags = target.AudioFile?.Tags;
        return new TrackMetadata(
            trackId,
            status,
            target.AudioFile?.RelativePath,
            tags?.Title ?? row.Title,
            tags?.Artist ?? row.ArtistName,
            tags?.Album ?? row.AlbumTitle,
            tags?.AlbumArtist ?? row.AlbumArtistName,
            tags?.Year ?? row.Year,
            tags?.TrackNumber ?? row.TrackNumber,
            tags?.DiscNumber ?? row.DiscNumber,
            tags?.Genre ?? row.Genre);
    }

    public async Task<TrackMetadataResult> UpdateAsync(
        Guid trackId,
        TagChanges changes,
        CancellationToken cancellationToken = default)
    {
        TrackEditTarget? target = await repository.FindEditTargetAsync(trackId, cancellationToken);
        if (target is null)
        {
            return TrackMetadataResult.Failure(TrackMetadataError.TrackNotFound);
        }

        if (target.AudioFile is not { } audio)
        {
            return TrackMetadataResult.Failure(TrackMetadataError.NoAudio);
        }

        TagChanges normalized = Normalize(changes);
        using (await writeLock.AcquireAsync(cancellationToken))
        {
            (TrackMetadataStatus status, string? fullPath) = await EvaluateAsync(audio, cancellationToken);
            if (status != TrackMetadataStatus.Editable || fullPath is null)
            {
                return TrackMetadataResult.Failure(TrackMetadataError.NotWritable);
            }

            if (!normalized.IsEmpty
                && !await WriteAsync(audio, fullPath, normalized, cancellationToken))
            {
                return TrackMetadataResult.Failure(TrackMetadataError.NotWritable);
            }
        }

        if (!normalized.IsEmpty)
        {
            // The file is written: the library follows it even if the client is gone by now.
            await scanner.RefreshFolderAsync(target.DirectoryPath, [audio.RelativePath], CancellationToken.None);
            await TakeOverSpellingAsync(normalized.Artist);
            await TakeOverSpellingAsync(normalized.AlbumArtist);
        }

        TrackMetadata? metadata = await GetAsync(trackId, CancellationToken.None);
        return metadata is null
            ? TrackMetadataResult.Failure(TrackMetadataError.TrackNotFound)
            : TrackMetadataResult.Success(metadata);
    }

    /// <summary>
    /// The changes as the scan would store them: texts normalized (an empty text removes the field), the genre cut to
    /// the length of the library, numbers below 1 removed.
    /// </summary>
    internal static TagChanges Normalize(TagChanges changes)
    {
        return new TagChanges
        {
            Title = Text(changes.Title),
            Artist = Text(changes.Artist),
            Album = Text(changes.Album),
            AlbumArtist = Text(changes.AlbumArtist),
            Year = Number(changes.Year),
            TrackNumber = Number(changes.TrackNumber),
            DiscNumber = Number(changes.DiscNumber),
            Genre = Text(changes.Genre, GenreMaxLength),
        };
    }

    /// <summary>
    /// The backup of the tags of a file. PostgreSQL does not allow NUL characters in texts, so they are removed.
    /// </summary>
    internal static TagBackup ToBackup(TagSnapshot snapshot, MediaFile file, DateTimeOffset now)
    {
        return new TagBackup
        {
            Id = Guid.CreateVersion7(now),
            MediaFileId = file.Id,
            RelativePath = file.RelativePath,
            Title = WithoutNul(snapshot.Title),
            Performers = [.. snapshot.Performers.Select(value => WithoutNul(value)!)],
            AlbumArtists = [.. snapshot.AlbumArtists.Select(value => WithoutNul(value)!)],
            Album = WithoutNul(snapshot.Album),
            Year = snapshot.Year,
            TrackNumber = snapshot.TrackNumber,
            TrackCount = snapshot.TrackCount,
            DiscNumber = snapshot.DiscNumber,
            DiscCount = snapshot.DiscCount,
            Genres = [.. snapshot.Genres.Select(value => WithoutNul(value)!)],
            CreatedAt = now,
        };
    }

    /// <summary>
    /// Whether the editor can write the audio file, checked on disk: a stored flag would miss a media folder that is
    /// mounted writable later, since that changes neither size nor change time of the file. A flag that differs is
    /// stored. Returns the full path of a file that exists.
    /// </summary>
    private async Task<(TrackMetadataStatus Status, string? FullPath)> EvaluateAsync(
        MediaFile? audio,
        CancellationToken cancellationToken)
    {
        if (audio is null)
        {
            return (TrackMetadataStatus.NoAudio, null);
        }

        if (audio.Issue is MediaFileIssue.TagsUnreadable or MediaFileIssue.Unreadable
            || !locator.TryGetFullPath(audio.RelativePath, out string fullPath)
            || !File.Exists(fullPath))
        {
            return (TrackMetadataStatus.NotWritable, null);
        }

        bool writable = writeAccess.IsWritable(fullPath);
        if (writable != audio.IsWritable)
        {
            await repository.SetWritableAsync(audio.Id, writable, cancellationToken);
        }

        return (writable ? TrackMetadataStatus.Editable : TrackMetadataStatus.NotWritable, fullPath);
    }

    /// <summary>
    /// Saves the current tags and writes the changes. Returns whether the file was written.
    /// </summary>
    private async Task<bool> WriteAsync(
        MediaFile audio,
        string fullPath,
        TagChanges changes,
        CancellationToken cancellationToken)
    {
        TagSnapshotResult snapshot = tagWriter.ReadSnapshot(fullPath);
        if (!snapshot.IsRead)
        {
            logger.LogWarning(
                "The tags of {File} cannot be read ({Status}); nothing was written",
                audio.RelativePath,
                snapshot.Status);
            await repository.SetWritableAsync(audio.Id, false, cancellationToken);
            return false;
        }

        await repository.AddTagBackupAsync(
            ToBackup(snapshot.Snapshot, audio, timeProvider.GetUtcNow()),
            cancellationToken);

        TagWriteStatus status = tagWriter.Write(fullPath, MediaFileKind.Audio, changes);
        if (status != TagWriteStatus.Written)
        {
            logger.LogWarning("The tags of {File} could not be written: {Status}", audio.RelativePath, status);
            await repository.SetWritableAsync(audio.Id, false, CancellationToken.None);
            return false;
        }

        logger.LogInformation("Tags of {File} written by the metadata editor", audio.RelativePath);
        return true;
    }

    private async Task TakeOverSpellingAsync(TagChange<string?>? change)
    {
        if (change?.Value is { } name && MetadataText.ToKey(name) is { } key)
        {
            await repository.RenameArtistAsync(key, name, CancellationToken.None);
        }
    }

    private static TagChange<string?>? Text(TagChange<string?>? change, int maxLength = MetadataText.MaxLength)
    {
        if (change is not { } value)
        {
            return null;
        }

        string? text = MetadataText.Normalize(value.Value);
        if (text is not null && text.Length > maxLength)
        {
            int length = char.IsHighSurrogate(text[maxLength - 1]) ? maxLength - 1 : maxLength;
            text = text[..length].TrimEnd();
        }

        return new TagChange<string?>(text);
    }

    private static TagChange<int?>? Number(TagChange<int?>? change)
    {
        return change is { } value ? new TagChange<int?>(value.Value is > 0 ? value.Value : null) : null;
    }

    private static string? WithoutNul(string? text)
    {
        return text?.Replace("\0", string.Empty, StringComparison.Ordinal);
    }
}
