using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BlockCount;

public static class BlockCountConfigurationReader
{
    public const string BlocksAlias = "blocks";

    public const string BlockCountAlias = "blockCount";

    public static IReadOnlyList<BlockCountRule> Read(IDictionary<string, object>? configurationData)
    {
        if (configurationData is null)
        {
            return [];
        }

        object? blocks = null;
        foreach (var pair in configurationData)
        {
            if (string.Equals(pair.Key, BlocksAlias, StringComparison.OrdinalIgnoreCase))
            {
                blocks = pair.Value;
                break;
            }
        }

        if (BlockCountJson.ToNode(blocks) is not JsonArray array)
        {
            return [];
        }

        var rules = new List<BlockCountRule>();
        foreach (var item in array)
        {
            if (item is not JsonObject block)
            {
                continue;
            }

            if (!BlockCountJson.TryGuid(BlockCountJson.Property(block, "contentElementTypeKey"), out var contentTypeKey))
            {
                continue;
            }

            if (BlockCountJson.Property(block, BlockCountAlias) is not JsonObject count)
            {
                continue;
            }

            rules.Add(new BlockCountRule
            {
                ContentElementTypeKey = contentTypeKey,
                Min = BlockCountJson.ReadInt(BlockCountJson.Property(count, "min")),
                Max = BlockCountJson.ReadInt(BlockCountJson.Property(count, "max")),
            });
        }

        return rules;
    }
}

internal static class BlockCountJson
{
    public static JsonNode? ToNode(object? value)
    {
        switch (value)
        {
            case null:
                return null;
            case JsonNode node:
                return node;
            case JsonElement element when element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null:
                return null;
            case JsonElement element:
                return JsonNode.Parse(element.GetRawText());
            case string text when string.IsNullOrWhiteSpace(text):
                return null;
            case string text:
                try
                {
                    return JsonNode.Parse(text);
                }
                catch (JsonException)
                {
                    return null;
                }
            default:
                return JsonSerializer.SerializeToNode(value);
        }
    }

    public static JsonNode? NormalizeBlockValue(JsonNode? value)
    {
        if (value is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var text))
        {
            return ToNode(text);
        }

        return value;
    }

    public static JsonNode? Property(JsonObject source, string name)
    {
        if (source.TryGetPropertyValue(name, out var value))
        {
            return value;
        }

        foreach (var pair in source)
        {
            if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Value;
            }
        }

        return null;
    }

    public static bool TryGuid(JsonNode? node, out Guid key)
    {
        key = default;
        if (node is not JsonValue value)
        {
            return false;
        }

        if (value.TryGetValue<Guid>(out key))
        {
            return true;
        }

        return value.TryGetValue<string>(out var text) && Guid.TryParse(text, out key);
    }

    public static int? ReadInt(JsonNode? node)
    {
        if (node is not JsonValue value)
        {
            return null;
        }

        if (value.TryGetValue<int>(out var number))
        {
            return number;
        }

        if (value.TryGetValue<long>(out var longNumber))
        {
            return checked((int)longNumber);
        }

        if (value.TryGetValue<double>(out var doubleNumber)
            && doubleNumber == Math.Truncate(doubleNumber)
            && doubleNumber is >= int.MinValue and <= int.MaxValue)
        {
            return (int)doubleNumber;
        }

        if (value.TryGetValue<string>(out var text)
            && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    public static string? ReadString(JsonNode? node)
    {
        if (node is not JsonValue value)
        {
            return null;
        }

        if (!value.TryGetValue<string>(out var text) || string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return text;
    }
}
