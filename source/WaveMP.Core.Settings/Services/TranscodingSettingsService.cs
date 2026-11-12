using Microsoft.Extensions.Logging;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Handlers;
using WaveMP.Core.Settings.Snapshots;
using WaveMP.Core.Settings.Stores;
using WaveMP.Core.Settings.Validations;

namespace WaveMP.Core.Settings.Services;

internal sealed class TranscodingSettingsService(
    ISettingsStore store,
    SettingsSnapshot<TranscodingSettings> snapshot,
    IEnumerable<ISettingsChangeHandler<TranscodingSettings>> handlers,
    ILogger<TranscodingSettingsService> logger) : ITranscodingSettingsService
{
    public async Task<TranscodingSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        TranscodingSettings stored =
            await store.GetAsync<TranscodingSettings>(TranscodingSettings.Key, cancellationToken);
        if (TranscodingSettingsValidator.Validate(stored).Count == 0)
        {
            return stored;
        }

        // Only possible if the stored document was changed outside of WaveMP.
        logger.LogWarning("The saved transcoding settings are invalid; using the defaults");
        return new TranscodingSettings();
    }

    public async Task<TranscodingSettingsResult> UpdateAsync(
        UpdateTranscodingSettingsCommand command,
        CancellationToken cancellationToken = default)
    {
        await snapshot.UpdateLock.WaitAsync(cancellationToken);
        try
        {
            TranscodingSettings current = await GetAsync(cancellationToken);
            TranscodingSettings changed = new()
            {
                HardwareAcceleration = command.HardwareAcceleration ?? current.HardwareAcceleration,
                MaxParallelJobs = command.MaxParallelJobs ?? current.MaxParallelJobs,
                MaxVideoHeight = command.MaxVideoHeight ?? current.MaxVideoHeight,
                AudioBitrateKbps = command.AudioBitrateKbps ?? current.AudioBitrateKbps,
            };

            IReadOnlyList<TranscodingSettingsValidationError> errors = TranscodingSettingsValidator.Validate(changed);
            if (errors.Count > 0)
            {
                return TranscodingSettingsResult.Failure(errors);
            }

            await store.SaveAsync(TranscodingSettings.Key, changed, cancellationToken);
            TranscodingSettings previous = snapshot.Current;
            snapshot.Set(changed);

            foreach (ISettingsChangeHandler<TranscodingSettings> handler in handlers)
            {
                try
                {
                    await handler.OnSettingsChangedAsync(previous, changed, cancellationToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError(
                        exception,
                        "{Handler} failed to apply the changed transcoding settings",
                        handler.GetType().Name);
                }
            }

            return TranscodingSettingsResult.Success(changed);
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
