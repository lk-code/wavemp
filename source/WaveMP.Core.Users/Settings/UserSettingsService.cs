using WaveMP.Data.Entities;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Users.Settings;

internal sealed class UserSettingsService(IUserRepository users) : IUserSettingsService
{
    public async Task<UserSettings?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        User? user = await users.FindAsync(userId, cancellationToken);
        return user is null ? null : user.Settings ?? new UserSettings { UserId = user.Id };
    }

    public async Task<UserSettings?> UpdateAsync(
        Guid userId,
        UpdateUserSettingsCommand command,
        CancellationToken cancellationToken = default)
    {
        User? user = await users.FindAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        UserSettings settings = user.Settings ??= new UserSettings { UserId = user.Id };
        if (command.Language is { } language)
        {
            settings.Language = language;
        }

        await users.UpdateAsync(user, cancellationToken);
        return settings;
    }
}
