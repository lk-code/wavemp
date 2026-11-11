using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using WaveMP.Service.Dtos;

namespace WaveMP.Service.Problems;

/// <summary>
/// A validation error as code with its parameters. The model state of ASP.NET Core only keeps texts per field, so the
/// error travels there as an internal message (see <see cref="ToMessage"/>) and becomes a
/// <see cref="ValidationErrorDto"/> again when the response is written (<see cref="ValidationProblems"/>).
/// </summary>
public readonly record struct ValidationFailure(ValidationErrorCodeDto Code, int? Min = null, int? Max = null)
{
    private const string Prefix = "validation:";
    private const char Separator = ';';
    private const string MinKey = "min=";
    private const string MaxKey = "max=";

    /// <summary>
    /// The internal message for the model state, e.g. <c>validation:OutOfRange;min=1;max=100</c>. It never leaves the
    /// server.
    /// </summary>
    public string ToMessage()
    {
        StringBuilder message = new(Prefix);
        message.Append(Code.ToString());
        if (Min is { } min)
        {
            message.Append(Separator).Append(MinKey).Append(min.ToString(CultureInfo.InvariantCulture));
        }

        if (Max is { } max)
        {
            message.Append(Separator).Append(MaxKey).Append(max.ToString(CultureInfo.InvariantCulture));
        }

        return message.ToString();
    }

    /// <summary>
    /// Reads a message written by <see cref="ToMessage"/>. Returns <c>false</c> for any other text, e.g. a message of
    /// the framework.
    /// </summary>
    public static bool TryParse([NotNullWhen(true)] string? message, out ValidationFailure failure)
    {
        failure = default;
        if (message is null || !message.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        string[] parts = message[Prefix.Length..].Split(Separator);
        if (!Enum.TryParse(parts[0], ignoreCase: false, out ValidationErrorCodeDto code)
            || !Enum.IsDefined(code)
            || !parts[0].All(char.IsAsciiLetter))
        {
            return false;
        }

        int? min = null;
        int? max = null;
        foreach (string part in parts.Skip(1))
        {
            if (part.StartsWith(MinKey, StringComparison.Ordinal) && TryParseNumber(part[MinKey.Length..], out int value))
            {
                min = value;
            }
            else if (part.StartsWith(MaxKey, StringComparison.Ordinal)
                && TryParseNumber(part[MaxKey.Length..], out value))
            {
                max = value;
            }
            else
            {
                return false;
            }
        }

        failure = new ValidationFailure(code, min, max);
        return true;
    }

    private static bool TryParseNumber(string text, out int value)
    {
        return int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    }
}
