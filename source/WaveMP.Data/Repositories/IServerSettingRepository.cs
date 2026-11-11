namespace WaveMP.Data.Repositories;

/// <summary>
/// Access to the groups of server settings, each stored as one JSON document.
/// </summary>
public interface IServerSettingRepository
{
    /// <summary>
    /// The JSON of the group, or <c>null</c> if it was never saved.
    /// </summary>
    Task<string?> FindAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the JSON of the group, replacing a saved one.
    /// </summary>
    Task UpsertAsync(string key, string json, DateTimeOffset updatedAt, CancellationToken cancellationToken = default);
}
