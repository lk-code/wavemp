using WaveMP.Core.Users.Profiles;
using WaveMP.Data.Entities;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Users.Users;

internal sealed class UserService(IUserRepository users, TimeProvider timeProvider) : IUserService
{
    public Task<Page<User>> ListAsync(UserListQuery query, CancellationToken cancellationToken = default)
    {
        return users.ListAsync(query, cancellationToken);
    }

    public Task<Page<User>> ListProfilesAsync(int offset, int limit, CancellationToken cancellationToken = default)
    {
        return users.ListProfilesAsync(offset, limit, cancellationToken);
    }

    public Task<User?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return users.FindAsync(id, cancellationToken);
    }

    public async Task<UserResult> CreateAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        if (!ProfileNames.TryNormalize(command.Name, out string name))
        {
            return UserResult.Failure(UserError.InvalidName);
        }

        string initials;
        if (string.IsNullOrWhiteSpace(command.Initials))
        {
            initials = InitialsGenerator.FromName(name);
        }
        else if (!ProfileInitials.TryNormalize(command.Initials, out initials))
        {
            return UserResult.Failure(UserError.InvalidInitials);
        }

        string color;
        if (command.Color is null)
        {
            color = ProfileColors.PickDefault(await users.ListColorsAsync(cancellationToken));
        }
        else if (ProfileColors.IsValid(command.Color))
        {
            color = ProfileColors.Normalize(command.Color);
        }
        else
        {
            return UserResult.Failure(UserError.InvalidColor);
        }

        string normalizedName = ProfileNames.ToNormalized(name);
        if (await users.NameExistsAsync(normalizedName, cancellationToken: cancellationToken))
        {
            return UserResult.Failure(UserError.NameTaken);
        }

        User user = new()
        {
            Name = name,
            NormalizedName = normalizedName,
            Initials = initials,
            Color = color,
            Role = command.Role ?? UserRole.User,
            IsBuiltIn = false,
            CreatedAt = timeProvider.GetUtcNow(),
            Settings = new UserSettings { Language = Language.En },
        };

        try
        {
            await users.AddAsync(user, cancellationToken);
        }
        catch (UserNameConflictException)
        {
            // Another request created a user with the same name in the meantime.
            return UserResult.Failure(UserError.NameTaken);
        }

        return UserResult.Success(user);
    }

    public async Task<UserResult> UpdateAsync(
        Guid id,
        UpdateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        User? user = await users.FindAsync(id, cancellationToken);
        if (user is null)
        {
            return UserResult.Failure(UserError.NotFound);
        }

        if (user.IsBuiltIn && command.Role is { } role && role != UserRole.Admin)
        {
            return UserResult.Failure(UserError.BuiltInAdmin);
        }

        string? name = null;
        if (command.Name is not null && !ProfileNames.TryNormalize(command.Name, out name))
        {
            return UserResult.Failure(UserError.InvalidName);
        }

        // An empty string derives the initials from the name again.
        string? initials = null;
        if (command.Initials is { Length: > 0 } && !ProfileInitials.TryNormalize(command.Initials, out initials))
        {
            return UserResult.Failure(UserError.InvalidInitials);
        }

        if (command.Color is not null && !ProfileColors.IsValid(command.Color))
        {
            return UserResult.Failure(UserError.InvalidColor);
        }

        if (name is not null && name != user.Name)
        {
            string normalizedName = ProfileNames.ToNormalized(name);
            if (await users.NameExistsAsync(normalizedName, user.Id, cancellationToken))
            {
                return UserResult.Failure(UserError.NameTaken);
            }

            // Initials derived from the old name follow the new name; own initials are kept.
            if (command.Initials is null && user.Initials == InitialsGenerator.FromName(user.Name))
            {
                user.Initials = InitialsGenerator.FromName(name);
            }

            user.Name = name;
            user.NormalizedName = normalizedName;
        }

        if (command.Initials is not null)
        {
            user.Initials = initials ?? InitialsGenerator.FromName(user.Name);
        }

        if (command.Color is not null)
        {
            user.Color = ProfileColors.Normalize(command.Color);
        }

        if (command.Role is { } newRole)
        {
            user.Role = newRole;
        }

        try
        {
            await users.UpdateAsync(user, cancellationToken);
        }
        catch (UserNameConflictException)
        {
            return UserResult.Failure(UserError.NameTaken);
        }

        return UserResult.Success(user);
    }

    public async Task<UserError?> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        User? user = await users.FindAsync(id, cancellationToken);
        if (user is null)
        {
            return UserError.NotFound;
        }

        if (user.IsBuiltIn)
        {
            return UserError.BuiltInAdmin;
        }

        await users.DeleteAsync(user, cancellationToken);
        return null;
    }
}
