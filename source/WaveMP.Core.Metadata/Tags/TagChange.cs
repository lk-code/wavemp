namespace WaveMP.Core.Metadata.Tags;

/// <summary>
/// The new value of a tag field; <c>null</c> removes the field.
/// </summary>
public readonly record struct TagChange<T>(T Value);
