using Microsoft.EntityFrameworkCore;
using WaveMP.Data.Entities;

namespace WaveMP.Data.Repositories;

internal sealed class ServerSettingRepository(WaveMpDbContext dbContext) : IServerSettingRepository
{
    public Task<string?> FindAsync(string key, CancellationToken cancellationToken = default)
    {
        return dbContext.ServerSettings.AsNoTracking()
            .Where(setting => setting.Key == key)
            .Select(setting => setting.Value)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpsertAsync(
        string key,
        string json,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default)
    {
        ServerSetting? setting = await dbContext.ServerSettings.FindAsync([key], cancellationToken);
        if (setting is null)
        {
            dbContext.ServerSettings.Add(new ServerSetting { Key = key, Value = json, UpdatedAt = updatedAt });
        }
        else
        {
            setting.Value = json;
            setting.UpdatedAt = updatedAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
