using Microsoft.EntityFrameworkCore;
using Npgsql;
using WaveMP.Data.Configurations;
using WaveMP.Data.Entities;
using WaveMP.Data.Queries;

namespace WaveMP.Data.Repositories;

internal sealed class UserRepository(WaveMpDbContext dbContext) : IUserRepository
{
    public Task<Page<User>> ListAsync(UserListQuery query, CancellationToken cancellationToken = default)
    {
        IQueryable<User> users = dbContext.Users.AsNoTracking();
        if (query.Role is { } role)
        {
            users = users.Where(user => user.Role == role);
        }

        IOrderedQueryable<User> sorted = query.Sort == UserSortField.CreatedAt
            ? users.OrderBy(user => user.CreatedAt, query.Direction)
            : users.OrderBy(user => user.NormalizedName, query.Direction);

        return sorted
            .ThenBy(user => user.Id, query.Direction)
            .ToPageAsync(query.Offset, query.Limit, cancellationToken);
    }

    public Task<Page<User>> ListProfilesAsync(int offset, int limit, CancellationToken cancellationToken = default)
    {
        return dbContext.Users.AsNoTracking()
            .OrderBy(user => user.IsBuiltIn)
            .ThenBy(user => user.CreatedAt)
            .ThenBy(user => user.Id)
            .ToPageAsync(offset, limit, cancellationToken);
    }

    public Task<User?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Users
            .Include(user => user.Settings)
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public Task<bool> NameExistsAsync(
        string normalizedName,
        Guid? exceptId = null,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Users.AnyAsync(
            user => user.NormalizedName == normalizedName && user.Id != exceptId,
            cancellationToken);
    }

    public Task<bool> BuiltInAdminExistsAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Users.AnyAsync(user => user.IsBuiltIn, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ListColorsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Users.Select(user => user.Color).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        dbContext.Users.Add(user);
        await SaveChangesAsync(cancellationToken);
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        return SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(User user, CancellationToken cancellationToken = default)
    {
        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: UserConfiguration.NormalizedNameIndex,
        })
        {
            // Nothing of the failed change stays tracked for later saves of this scope.
            dbContext.ChangeTracker.Clear();
            throw new UserNameConflictException(exception);
        }
    }
}
