namespace WaveMP.Data.Entities;

/// <summary>
/// Session of a profile, created when the profile is selected. Its id is part of the token, so ending or deleting
/// the session invalidates the token at once.
/// </summary>
public class Session
{
    public Guid Id { get; set; }

    public required Guid UserId { get; set; }

    public User? User { get; set; }

    public required DateTimeOffset CreatedAt { get; set; }

    public required DateTimeOffset ExpiresAt { get; set; }
}
