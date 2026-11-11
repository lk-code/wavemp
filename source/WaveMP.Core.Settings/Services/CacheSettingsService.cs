using Microsoft.Extensions.Logging;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Handlers;
using WaveMP.Core.Settings.Snapshots;
using WaveMP.Core.Settings.Stores;
using WaveMP.Core.Settings.Validations;

namespace WaveMP.Core.Settings.Services;

internal sealed class CacheSettingsService(
    ISettingsStore store,
    SettingsSnapshot<CacheSettings> snapshot,
    IEnumerable<ISettingsChangeHandler<CacheSettings>> handlers,
    ILogger<CacheSettingsService> logger) : ICacheSettingsService
{
    public async Task<CacheSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        CacheSettings stored = await store.GetAsync<CacheSettings>(CacheSettings.Key, cancellationToken);
        if (CacheSettingsValidator.Validate(stored).Count == 0)
        {
            return stored;
        }

        // Only possible if the stored document was changed outside of WaveMP.
        logger.LogWarning("The saved cache settings are invalid; using the defaults");
        return new CacheSettings();
    }

    public async Task<CacheSettingsResult> UpdateAsync(
        UpdateCacheSettingsCommand command,
        CancellationToken cancellationToken = default)
    {
        await snapshot.UpdateLock.WaitAsync(cancellationToken);
        try
        {
            CacheSettings current = await GetAsync(cancellationToken);
            CacheSettings changed = new()
            {
                MaxSizeMegabytes = command.MaxSizeMegabytes ?? current.MaxSizeMegabytes,
            };

            IReadOnlyList<CacheSettingsValidationError> errors = CacheSettingsValidator.Validate(changed);
            if (errors.Count > 0)
            {
                return CacheSettingsResult.Failure(errors);
            }

            await store.SaveAsync(CacheSettings.Key, changed, cancellationToken);
            CacheSettings previous = snapshot.Current;
            snapshot.Set(changed);

            foreach (ISettingsChangeHandler<CacheSettings> handler in handlers)
            {
                try
                {
                    await handler.OnSettingsChangedAsync(previous, changed, cancellationToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError(
                        exception,
                        "{Handler} failed to apply the changed cache settings",
                        handler.GetType().Name);
                }
            }

            return CacheSettingsResult.Success(changed);
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
