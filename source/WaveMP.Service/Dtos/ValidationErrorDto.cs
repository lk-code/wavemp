using System.Text.Json.Serialization;

namespace WaveMP.Service.Dtos;

/// <summary>
/// A rejected field of a request: where, why, and the limits that apply.
/// </summary>
public sealed class ValidationErrorDto
{
    /// <summary>
    /// The field: the JSON name of a member of the body (nested members as <c>a.b</c>, entries of a list as
    /// <c>a[0]</c>) or the name of a query parameter. Empty for the body as a whole, e.g. a missing body or malformed
    /// JSON.
    /// </summary>
    public required string Field { get; init; }

    public required ValidationErrorCodeDto Code { get; init; }

    /// <summary>
    /// Lower limit for <c>min_length</c>, <c>min_items</c> and <c>out_of_range</c>; missing for other codes.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Min { get; init; }

    /// <summary>
    /// Upper limit for <c>max_length</c>, <c>max_items</c> and <c>out_of_range</c>; missing for other codes.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Max { get; init; }
}
