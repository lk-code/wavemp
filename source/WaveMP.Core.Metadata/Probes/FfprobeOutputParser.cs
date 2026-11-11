using System.Globalization;
using System.Text.Json;

namespace WaveMP.Core.Metadata.Probes;

/// <summary>
/// Reads the JSON output of <c>ffprobe -print_format json -show_format -show_streams</c>. ffprobe writes many
/// numbers as strings (e.g. <c>"bit_rate": "320000"</c>) and leaves out unknown values, so every value is read
/// leniently.
/// </summary>
internal static class FfprobeOutputParser
{
    /// <summary>
    /// The technical information, or <c>null</c> if the output is no valid ffprobe JSON.
    /// </summary>
    public static MediaProbeResult? Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            JsonElement format = root.TryGetProperty("format", out JsonElement value)
                && value.ValueKind == JsonValueKind.Object
                    ? value
                    : default;
            JsonElement[] streams = root.TryGetProperty("streams", out JsonElement list)
                && list.ValueKind == JsonValueKind.Array
                    ? [.. list.EnumerateArray().Where(stream => stream.ValueKind == JsonValueKind.Object)]
                    : [];

            JsonElement? audio = streams.Cast<JsonElement?>()
                .FirstOrDefault(stream => ReadString(stream!.Value, "codec_type") == "audio");
            JsonElement? video = streams.Cast<JsonElement?>()
                .FirstOrDefault(stream => ReadString(stream!.Value, "codec_type") == "video" && !IsAttachedPicture(stream.Value));
            JsonElement? picture = streams.Cast<JsonElement?>()
                .FirstOrDefault(stream => ReadString(stream!.Value, "codec_type") == "video" && IsAttachedPicture(stream.Value));

            Dictionary<string, string> tags = new(StringComparer.Ordinal);
            AddTags(tags, format);
            if (audio is { } audioStream)
            {
                AddTags(tags, audioStream);
            }

            double? seconds = ReadDouble(format, "duration");
            return new MediaProbeResult(
                ReadString(format, "format_name"),
                seconds is >= 0 and < 1e9 ? TimeSpan.FromSeconds(seconds.Value) : null,
                ReadLong(format, "bit_rate"),
                tags,
                audio is { } a
                    ? new AudioStreamInfo(
                        ReadString(a, "codec_name"),
                        ReadLong(a, "bit_rate"),
                        ReadInt(a, "sample_rate"),
                        ReadInt(a, "channels"),
                        Positive(ReadInt(a, "bits_per_raw_sample")) ?? Positive(ReadInt(a, "bits_per_sample")))
                    : null,
                video is { } v
                    ? new VideoStreamInfo(
                        ReadString(v, "codec_name"),
                        ReadInt(v, "width"),
                        ReadInt(v, "height"),
                        ReadLong(v, "bit_rate"))
                    : null,
                picture is { } p
                    ? new PictureStreamInfo(ReadString(p, "codec_name"), ReadInt(p, "width"), ReadInt(p, "height"))
                    : null);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool IsAttachedPicture(JsonElement stream)
    {
        return stream.TryGetProperty("disposition", out JsonElement disposition)
            && disposition.ValueKind == JsonValueKind.Object
            && ReadInt(disposition, "attached_pic") == 1;
    }

    private static void AddTags(Dictionary<string, string> tags, JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty("tags", out JsonElement values)
            || values.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (JsonProperty tag in values.EnumerateObject())
        {
            if (tag.Value.ValueKind == JsonValueKind.String)
            {
                tags.TryAdd(tag.Name.ToLowerInvariant(), tag.Value.GetString()!);
            }
        }
    }

    private static string? ReadString(JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(name, out JsonElement value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
    }

    private static double? ReadDouble(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out JsonElement value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number => value.GetDouble(),
            JsonValueKind.String when double.TryParse(
                value.GetString(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double parsed) => parsed,
            _ => null,
        };
    }

    private static long? ReadLong(JsonElement element, string name)
    {
        double? value = ReadDouble(element, name);
        return value is >= 0 and <= long.MaxValue ? (long)value.Value : null;
    }

    private static int? ReadInt(JsonElement element, string name)
    {
        double? value = ReadDouble(element, name);
        return value is >= int.MinValue and <= int.MaxValue ? (int)value.Value : null;
    }

    private static int? Positive(int? value)
    {
        return value > 0 ? value : null;
    }
}
