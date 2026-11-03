using System.Text.Json.Nodes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BlockCount;

internal static class BlockCountEditorValues
{
    public static BlockCountLimit[] Read(IDictionary<string, object?>? editorValues)
    {
        if (editorValues is null || !TryGet(editorValues.Select(pair => Key(pair.Key, pair.Value)), "blocks", out var blocks) || blocks is null)
        {
            return [];
        }

        var rules = BlockCountConfigurationReader.Read(new Dictionary<string, object>
        {
            [BlockCountConfigurationReader.BlocksAlias] = Normalize(blocks),
        });

        return rules.Select(rule => new BlockCountLimit
        {
            ContentElementTypeKey = rule.ContentElementTypeKey,
            Min = rule.Min,
            Max = rule.Max,
        }).ToArray();
    }

    public static void Apply(IDictionary<string, object> editor, IReadOnlyList<BlockCountLimit>? limits)
    {
        if (limits is null || limits.Count == 0 || !TryGet(editor.Select(pair => Key(pair.Key, pair.Value)), "blocks", out var blocks) || blocks is null)
        {
            return;
        }

        var token = ToToken(blocks);
        if (token is not JArray array)
        {
            return;
        }

        var byKey = limits
            .Where(limit => limit.ContentElementTypeKey != Guid.Empty)
            .GroupBy(limit => limit.ContentElementTypeKey)
            .ToDictionary(group => group.Key, group => group.First());

        foreach (var item in array.OfType<JObject>())
        {
            var keyText = item["contentElementTypeKey"]?.ToString() ?? item["ContentElementTypeKey"]?.ToString();
            if (!Guid.TryParse(keyText, out var key) || !byKey.TryGetValue(key, out var limit))
            {
                continue;
            }

            if (limit.Min is null && limit.Max is null)
            {
                continue;
            }

            var count = new JObject();
            if (limit.Min is int minimum)
            {
                count["min"] = minimum;
            }

            if (limit.Max is int maximum)
            {
                count["max"] = maximum;
            }

            item["blockCount"] = count;
        }

        editor["blocks"] = array;
    }

    private static object Normalize(object blocks)
    {
        if (blocks is JToken token)
        {
            return token.ToString(Formatting.None);
        }

        if (blocks is JsonNode node)
        {
            return node;
        }

        return blocks;
    }

    private static JToken ToToken(object blocks)
    {
        switch (blocks)
        {
            case JToken token:
                return token;
            case JsonNode node:
                return JToken.Parse(node.ToJsonString());
            case string text:
                return JToken.Parse(text);
            default:
                return JToken.FromObject(blocks);
        }
    }

    private static KeyValuePair<string, object?> Key(string key, object? value) => new(key, value);

    private static bool TryGet(IEnumerable<KeyValuePair<string, object?>> values, string key, out object? value)
    {
        foreach (var pair in values)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        value = null;
        return false;
    }
}
