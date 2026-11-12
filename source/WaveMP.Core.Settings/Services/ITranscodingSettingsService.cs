using WaveMP.Core.Settings.Groups;

namespace WaveMP.Core.Settings.Services;

/// <summary>
/// Reads and changes the transcoding settings.
/// </summary>
public interface ITranscodingSettingsService
{
    Task<TranscodingSettings> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks and saves the changes. Valid changes apply at once: the snapshot is replaced and every
    /// <see cref="Handlers.ISettingsChangeHandler{T}"/> is told. Running transcodes keep their settings.
    /// </summary>
    Task<TranscodingSettingsResult> UpdateAsync(
        UpdateTranscodingSettingsCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the saved settings into the snapshot. Runs on start, after the migrations.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
