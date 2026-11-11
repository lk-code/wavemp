namespace WaveMP.Service.Dtos;

/// <summary>
/// A session of a profile.
/// </summary>
public sealed class SessionDto
{
    public required Guid Id { get; init; }

    /// <summary>
    /// The selected profile.
    /// </summary>
    public required ProfileDto Profile { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When the session and its token expire.
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; init; }
}
