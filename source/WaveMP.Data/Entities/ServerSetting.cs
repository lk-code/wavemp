namespace WaveMP.Data.Entities;

/// <summary>
/// A group of server settings (e.g. <c>library</c>), stored as one JSON document. New settings of a group need no
/// migration; missing values keep their defaults when the group is read.
/// </summary>
public class ServerSetting
{
    public required string Key { get; set; }

    /// <summary>
    /// The settings of the group as JSON (column type <c>jsonb</c>).
    /// </summary>
    public required string Value { get; set; }

    public required DateTimeOffset UpdatedAt { get; set; }
}
