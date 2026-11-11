namespace WaveMP.Core.Settings.Stores;

/// <summary>
/// Reads and saves settings groups as JSON documents.
/// </summary>
public interface ISettingsStore
{
    /// <summary>
    /// The saved group, or its defaults if it was never saved or cannot be read; missing values keep their defaults.
    /// </summary>
    Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class, new();

    Task SaveAsync<T>(string key, T value, CancellationToken cancellationToken = default)
        where T : class;
}
