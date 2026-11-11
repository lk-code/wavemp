using System.ComponentModel.DataAnnotations;

namespace WaveMP.Service.Validators;

/// <summary>
/// Like <see cref="RequiredAttribute"/>, but also rejects the default value of a value type, e.g. an empty
/// <see cref="Guid"/>. A missing member of a request body keeps its default value (see
/// <c>ServiceCollectionExtensions.AddWaveMpJson</c>), so this reports it as <c>required</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class RequiredValueAttribute : RequiredAttribute
{
    public override bool IsValid(object? value)
    {
        return base.IsValid(value) && !IsDefault(value!);
    }

    private static bool IsDefault(object value)
    {
        Type type = value.GetType();
        return type.IsValueType && value.Equals(Activator.CreateInstance(type));
    }
}
