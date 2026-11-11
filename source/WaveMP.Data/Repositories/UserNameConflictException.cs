namespace WaveMP.Data.Repositories;

/// <summary>
/// Thrown when a user is saved with a name that another user already has (unique index on the normalized name).
/// </summary>
public sealed class UserNameConflictException(Exception innerException)
    : Exception("The name is already used by another user.", innerException);
