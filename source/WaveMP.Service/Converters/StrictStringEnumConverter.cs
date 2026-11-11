using System.Text.Json.Serialization;

namespace WaveMP.Service.Converters;

/// <summary>
/// Reads and writes an enum only by its name. Unlike the defaults of <see cref="JsonStringEnumConverter{TEnum}"/>,
/// JSON numbers are rejected, so a request body with a number for an enum fails model binding (400
/// <c>request.invalid</c>) instead of passing an undefined value on. This matches the OpenAPI document, which
/// describes every enum as a string.
/// </summary>
public sealed class StrictStringEnumConverter<TEnum>()
    : JsonStringEnumConverter<TEnum>(namingPolicy: null, allowIntegerValues: false)
    where TEnum : struct, Enum
{
}
