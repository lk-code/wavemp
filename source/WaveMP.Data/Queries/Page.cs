namespace WaveMP.Data.Queries;

/// <summary>
/// One page of a collection: the items from <paramref name="Offset"/> on, at most <paramref name="Limit"/> of them,
/// and the total number of items of the whole collection.
/// </summary>
public sealed record Page<T>(IReadOnlyList<T> Items, int Offset, int Limit, int Total);
