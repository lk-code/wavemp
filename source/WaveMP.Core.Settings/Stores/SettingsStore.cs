using System.Text.Json;
using Microsoft.Extensions.Logging;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Settings.Stores;

internal sealed class SettingsStore(
    IServerSettingRepository repository,
    TimeProvider timeProvider,
    ILogger<SettingsStore> logger) : ISettingsStore
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class, new()
    {
        string? json = await repository.FindAsync(key, cancellationToken);
        if (json is null)
        {
            return new T();
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? new T();
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "The settings group {SettingsGroup} cannot be read; using the defaults", key);
            return new T();
        }
    }

    public Task SaveAsync<T>(string key, T value, CancellationToken cancellationToken = default)
        where T : class
    {
        string json = JsonSerializer.Serialize(value, JsonOptions);
        return repository.UpsertAsync(key, json, timeProvider.GetUtcNow(), cancellationToken);
    }
}
