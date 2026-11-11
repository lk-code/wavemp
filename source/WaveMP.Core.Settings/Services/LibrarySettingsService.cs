using Microsoft.Extensions.Logging;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Handlers;
using WaveMP.Core.Settings.Snapshots;
using WaveMP.Core.Settings.Stores;
using WaveMP.Core.Settings.Validations;

namespace WaveMP.Core.Settings.Services;

internal sealed class LibrarySettingsService(
    ISettingsStore store,
    SettingsSnapshot<LibrarySettings> snapshot,
    IEnumerable<ISettingsChangeHandler<LibrarySettings>> handlers,
    ILogger<LibrarySettingsService> logger) : ILibrarySettingsService
{
    public async Task<LibrarySettings> GetAsync(CancellationToken cancellationToken = default)
    {
        LibrarySettings stored = await store.GetAsync<LibrarySettings>(LibrarySettings.Key, cancellationToken);
        LibrarySettings normalized = LibrarySettingsValidator.Normalize(stored);
        if (LibrarySettingsValidator.Validate(normalized).Count == 0)
        {
            return normalized;
        }

        // Only possible if the stored document was changed outside of WaveMP.
        logger.LogWarning("The saved library settings are invalid; using the defaults");
        return new LibrarySettings();
    }

    public async Task<LibrarySettingsResult> UpdateAsync(
        UpdateLibrarySettingsCommand command,
        CancellationToken cancellationToken = default)
    {
        await snapshot.UpdateLock.WaitAsync(cancellationToken);
        try
        {
            LibrarySettings current = await GetAsync(cancellationToken);
            LibrarySettings changed = LibrarySettingsValidator.Normalize(new LibrarySettings
            {
                ScanIntervalMinutes = command.ScanIntervalMinutes ?? current.ScanIntervalMinutes,
                FileWatcherEnabled = command.FileWatcherEnabled ?? current.FileWatcherEnabled,
                AudioExtensions = command.AudioExtensions ?? current.AudioExtensions,
                VideoExtensions = command.VideoExtensions ?? current.VideoExtensions,
                ImageExtensions = command.ImageExtensions ?? current.ImageExtensions,
                AlbumCoverNames = command.AlbumCoverNames ?? current.AlbumCoverNames,
            });

            IReadOnlyList<SettingsValidationError> errors = LibrarySettingsValidator.Validate(changed);
            if (errors.Count > 0)
            {
                return LibrarySettingsResult.Failure(errors);
            }

            await store.SaveAsync(LibrarySettings.Key, changed, cancellationToken);
            LibrarySettings previous = snapshot.Current;
            snapshot.Set(changed);

            foreach (ISettingsChangeHandler<LibrarySettings> handler in handlers)
            {
                try
                {
                    await handler.OnSettingsChangedAsync(previous, changed, cancellationToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError(
                        exception,
                        "{Handler} failed to apply the changed library settings",
                        handler.GetType().Name);
                }
            }

            return LibrarySettingsResult.Success(changed);
        }
        finally
        {
            snapshot.UpdateLock.Release();
        }
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        snapshot.Set(await GetAsync(cancellationToken));
    }
}
