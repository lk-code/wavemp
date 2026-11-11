namespace WaveMP.Data.Entities;

/// <summary>
/// A profile of WaveMP. Profiles are selected without a password; the built-in admin profile is created on the first
/// start and can be neither deleted nor demoted.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    /// <summary>
    /// Display name, unique without regard to case (see <see cref="NormalizedName"/>).
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Upper-case form of <see cref="Name"/>; carries the unique index.
    /// </summary>
    public required string NormalizedName { get; set; }

    /// <summary>
    /// Short form of the name shown on the profile tile, by default derived from the name.
    /// </summary>
    public required string Initials { get; set; }

    /// <summary>
    /// Color of the profile as <c>#rrggbb</c> in lower case.
    /// </summary>
    public required string Color { get; set; }

    public required UserRole Role { get; set; }

    /// <summary>
    /// <c>true</c> for the built-in admin profile. A filtered unique index allows only one.
    /// </summary>
    public required bool IsBuiltIn { get; set; }

    /// <summary>
    /// Prepared for the optional PIN (roadmap step 20); not used yet.
    /// </summary>
    public string? PinHash { get; set; }

    public required DateTimeOffset CreatedAt { get; set; }

    public UserSettings? Settings { get; set; }

    public ICollection<Session> Sessions { get; set; } = [];
}
