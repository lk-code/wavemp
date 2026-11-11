using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WaveMP.Core.Users.Profiles;
using WaveMP.Data.Entities;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Users.Users;

/// <summary>
/// Creates the built-in admin profile on the first start. It cannot be deleted and always stays an admin, so there
/// is always a profile that can manage the others.
/// </summary>
public sealed class AdminProfileInitializer(
    IUserRepository users,
    TimeProvider timeProvider,
    ILogger<AdminProfileInitializer> logger)
{
    /// <summary>
    /// Name of the built-in admin profile. It is profile data like any other name and can be changed.
    /// </summary>
    public const string DefaultName = "Admin";

    public async Task EnsureAsync(CancellationToken cancellationToken = default)
    {
        if (await users.BuiltInAdminExistsAsync(cancellationToken))
        {
            return;
        }

        User admin = new()
        {
            Name = DefaultName,
            NormalizedName = ProfileNames.ToNormalized(DefaultName),
            Initials = InitialsGenerator.FromName(DefaultName),
            Color = ProfileColors.PickDefault([]),
            Role = UserRole.Admin,
            IsBuiltIn = true,
            CreatedAt = timeProvider.GetUtcNow(),
            Settings = new UserSettings { Language = Language.En },
        };

        try
        {
            await users.AddAsync(admin, cancellationToken);
        }
        catch (Exception exception) when (exception is UserNameConflictException or DbUpdateException)
        {
            // Another instance started at the same time and created the profile first.
            if (await users.BuiltInAdminExistsAsync(cancellationToken))
            {
                return;
            }

            throw;
        }

        logger.LogInformation("Created the built-in admin profile");
    }
}
