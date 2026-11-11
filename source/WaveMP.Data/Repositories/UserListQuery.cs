using WaveMP.Data.Entities;
using WaveMP.Data.Queries;

namespace WaveMP.Data.Repositories;

/// <summary>
/// Paging, sorting and filter of the user list.
/// </summary>
/// <param name="Offset">Number of users to skip.</param>
/// <param name="Limit">Maximum number of users of the page.</param>
/// <param name="Sort">Sort key; ties are sorted by id.</param>
/// <param name="Direction">Sort direction of the key and of the id.</param>
/// <param name="Role">Only users with this role; <c>null</c> for all users.</param>
public sealed record UserListQuery(
    int Offset,
    int Limit,
    UserSortField Sort = UserSortField.Name,
    SortDirection Direction = SortDirection.Asc,
    UserRole? Role = null);
