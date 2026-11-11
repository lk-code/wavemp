using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WaveMP.Core.Caching.Covers;
using WaveMP.Core.Caching.Images;
using WaveMP.Core.Library.Covers;
using WaveMP.Core.Playlists.Options;

namespace WaveMP.Core.Playlists.Covers;

internal sealed class PlaylistCoverStore(
    ICoverImageEncoder encoder,
    IOptions<PlaylistOptions> options,
    ILogger<PlaylistCoverStore> logger) : IPlaylistCoverStore
{
    internal const string TemporaryFolderName = ".tmp";
    internal const string FileExtension = ".webp";

    private static readonly string[] _uploadFormats = [ImageFormats.Jpeg, ImageFormats.Png, ImageFormats.WebP];

    private volatile bool _isAvailable;

    public bool IsAvailable => _isAvailable;

    private string RootPath => options.Value.RootPath;

    private string TemporaryPath => Path.Combine(RootPath, TemporaryFolderName);

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(RootPath);
            if (Directory.Exists(TemporaryPath))
            {
                Directory.Delete(TemporaryPath, recursive: true);
            }

            Directory.CreateDirectory(TemporaryPath);
            _isAvailable = true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _isAvailable = false;
            logger.LogWarning(
                exception,
                "The folder {Folder} of the playlist covers cannot be written; covers cannot be uploaded",
                RootPath);
        }

        return Task.CompletedTask;
    }

    public async Task<CoverSaveStatus> SaveAsync(
        Guid playlistId,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        MemoryStream? copy = null;
        Stream picture = content;
        if (!content.CanSeek)
        {
            copy = new MemoryStream();
            await content.CopyToAsync(copy, cancellationToken);
            picture = copy;
        }

        await using (copy)
        {
            return await SaveSeekableAsync(playlistId, picture, cancellationToken);
        }
    }

    private async Task<CoverSaveStatus> SaveSeekableAsync(
        Guid playlistId,
        Stream picture,
        CancellationToken cancellationToken)
    {
        picture.Position = 0;
        byte[] header = new byte[ImageFormats.HeaderLength];
        int read = await picture.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        string? contentType = ImageFormats.Detect(header.AsSpan(0, read));
        if (contentType is null || !_uploadFormats.Contains(contentType))
        {
            return CoverSaveStatus.UnsupportedFormat;
        }

        if (!_isAvailable)
        {
            return CoverSaveStatus.Unavailable;
        }

        picture.Position = 0;
        string prefix = Guid.NewGuid().ToString("N");
        CoverEncodeTarget[] targets = [.. CoverSizes.All.Select(size => new CoverEncodeTarget(
            size,
            Path.Combine(TemporaryPath, $"{prefix}-{size.ToString(CultureInfo.InvariantCulture)}{FileExtension}"),
            Square: true))];
        try
        {
            CoverEncodeStatus status = await encoder.EncodeAsync(picture, contentType, targets, cancellationToken);
            switch (status)
            {
                case CoverEncodeStatus.Unavailable:
                    return CoverSaveStatus.Unavailable;
                case CoverEncodeStatus.Failed:
                    return CoverSaveStatus.Unreadable;
            }

            string folder = FolderOf(playlistId);
            Directory.CreateDirectory(folder);
            foreach (CoverEncodeTarget target in targets)
            {
                File.Move(target.OutputPath, FileOf(playlistId, target.MaxSize), overwrite: true);
            }

            return CoverSaveStatus.Saved;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Cannot store the cover of the playlist {PlaylistId}", playlistId);
            return CoverSaveStatus.Unavailable;
        }
        finally
        {
            foreach (CoverEncodeTarget target in targets)
            {
                TryDeleteFile(target.OutputPath);
            }
        }
    }

    public Stream? Open(Guid playlistId, int size)
    {
        try
        {
            // Delete: a new upload may replace the file while it is delivered.
            return new FileStream(
                FileOf(playlistId, CoverSizes.Snap(size)),
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read | FileShare.Delete,
                bufferSize: 1,
                useAsync: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public void Delete(Guid playlistId)
    {
        string folder = FolderOf(playlistId);
        try
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Cannot remove the cover of the playlist {PlaylistId}", playlistId);
        }
    }

    public IReadOnlyList<Guid> ListPlaylistIds()
    {
        if (!Directory.Exists(RootPath))
        {
            return [];
        }

        try
        {
            return [.. Directory.EnumerateDirectories(RootPath)
                .Select(Path.GetFileName)
                .Select(name => Guid.TryParseExact(name, "D", out Guid id) ? id : Guid.Empty)
                .Where(id => id != Guid.Empty)];
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Cannot list the folder {Folder} of the playlist covers", RootPath);
            return [];
        }
    }

    private string FolderOf(Guid playlistId)
    {
        return Path.Combine(RootPath, playlistId.ToString("D"));
    }

    private string FileOf(Guid playlistId, int size)
    {
        return Path.Combine(
            FolderOf(playlistId),
            $"cover-{size.ToString(CultureInfo.InvariantCulture)}{FileExtension}");
    }

    private void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogDebug(exception, "Cannot remove the unfinished picture {Path}", path);
        }
    }
}
