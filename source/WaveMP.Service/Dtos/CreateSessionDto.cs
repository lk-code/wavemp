namespace WaveMP.Service.Dtos;

/// <summary>
/// Selects a profile.
/// </summary>
public sealed class CreateSessionDto
{
    /// <summary>
    /// Id of the profile to select.
    /// </summary>
    public required Guid ProfileId { get; init; }
}
