using WaveMP.Data.Entities;

namespace WaveMP.Core.Users.Settings;

/// <summary>
/// Reads and changes the settings of a user.
/// </summary>
public interface IUserSettingsService
{
    /// <summary>
    /// The settings of the user, or <c>null</c> if the user does not exist.
    /// </summary>
    Task<UserSettings?> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the settings of the user. Returns the saved settings, or <c>null</c> if the user does not exist.
    /// </summary>
    Task<UserSettings?> UpdateAsync(
        Guid userId,
        UpdateUserSettingsCommand command,
        CancellationToken cancellationToken = default);
}
