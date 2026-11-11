using WaveMP.Service.Validators;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Selects a profile.
/// </summary>
public sealed class CreateSessionDto
{
    /// <summary>
    /// Id of the profile to select.
    /// </summary>
    [RequiredValue]
    public required Guid ProfileId { get; init; }
}
