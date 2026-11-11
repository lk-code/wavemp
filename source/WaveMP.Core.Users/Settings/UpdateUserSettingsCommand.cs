using WaveMP.Data.Entities;

namespace WaveMP.Core.Users.Settings;

/// <summary>
/// Changes the settings of a user. Every <c>null</c> value keeps the current value.
/// </summary>
/// <param name="Language">Language of the user interface.</param>
public sealed record UpdateUserSettingsCommand(Language? Language = null);
