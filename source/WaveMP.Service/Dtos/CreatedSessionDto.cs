namespace WaveMP.Service.Dtos;

/// <summary>
/// A new session with its token.
/// </summary>
public sealed class CreatedSessionDto
{
    /// <summary>
    /// Bearer token of the session for the Authorization header.
    /// </summary>
    public required string Token { get; init; }

    public required SessionDto Session { get; init; }
}
