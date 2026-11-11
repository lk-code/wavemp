using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace WaveMP.Data.Queries;

public static class QueryableExtensions
{
    /// <summary>
    /// Loads one page of the query and counts all its items. The query must be sorted stably (e.g. by a unique key
    /// as last criterion), otherwise items may appear on more than one page.
    /// </summary>
    public static async Task<Page<T>> ToPageAsync<T>(
        this IQueryable<T> query,
        int offset,
        int limit,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

        int total = await query.CountAsync(cancellationToken);
        List<T> items = total > offset
            ? await query.Skip(offset).Take(limit).ToListAsync(cancellationToken)
            : [];

        return new Page<T>(items, offset, limit, total);
    }

    /// <summary>
    /// Sorts ascending or descending by the key.
    /// </summary>
    public static IOrderedQueryable<T> OrderBy<T, TKey>(
        this IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector,
        SortDirection direction)
    {
        return direction == SortDirection.Desc
            ? query.OrderByDescending(keySelector)
            : query.OrderBy(keySelector);
    }

    /// <summary>
    /// Adds a further sort key in the given direction.
    /// </summary>
    public static IOrderedQueryable<T> ThenBy<T, TKey>(
        this IOrderedQueryable<T> query,
        Expression<Func<T, TKey>> keySelector,
        SortDirection direction)
    {
        return direction == SortDirection.Desc
            ? query.ThenByDescending(keySelector)
            : query.ThenBy(keySelector);
    }
}
