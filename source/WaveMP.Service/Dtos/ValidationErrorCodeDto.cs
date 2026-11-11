using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Machine-readable reason why a field of a request was rejected, part of every validation problem. Clients translate
/// it; the parameters <c>min</c> and <c>max</c> of the error complete it where stated.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<ValidationErrorCodeDto>))]
public enum ValidationErrorCodeDto
{
    /// <summary>
    /// The value is missing, null or empty (also white space only), or the request has no body.
    /// </summary>
    [JsonStringEnumMemberName("required")]
    Required,

    /// <summary>
    /// The text is shorter than <c>min</c> characters (UTF-16 code units).
    /// </summary>
    [JsonStringEnumMemberName("min_length")]
    MinLength,

    /// <summary>
    /// The text is longer than <c>max</c> characters (UTF-16 code units).
    /// </summary>
    [JsonStringEnumMemberName("max_length")]
    MaxLength,

    /// <summary>
    /// The list has fewer than <c>min</c> entries.
    /// </summary>
    [JsonStringEnumMemberName("min_items")]
    MinItems,

    /// <summary>
    /// The list has more than <c>max</c> entries.
    /// </summary>
    [JsonStringEnumMemberName("max_items")]
    MaxItems,

    /// <summary>
    /// The number is outside the range from <c>min</c> to <c>max</c> (both included).
    /// </summary>
    [JsonStringEnumMemberName("out_of_range")]
    OutOfRange,

    /// <summary>
    /// The value has the wrong form or type, e.g. text for a number, malformed JSON, an unknown enum name, a color
    /// that is not #rrggbb or an entry of a list that breaks the rules of the list.
    /// </summary>
    [JsonStringEnumMemberName("invalid_format")]
    InvalidFormat,

    /// <summary>
    /// The value has the right form but is not allowed, e.g. an undefined number for an enum or a name that is not
    /// valid after normalization.
    /// </summary>
    [JsonStringEnumMemberName("invalid_value")]
    InvalidValue,

    /// <summary>
    /// The value is already used by another field, e.g. an extension that is in more than one list.
    /// </summary>
    [JsonStringEnumMemberName("duplicate")]
    Duplicate,
}
