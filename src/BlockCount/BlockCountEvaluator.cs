using System.Text.Json;
using System.Text.Json.Nodes;

namespace BlockCount;

public static class BlockCountEvaluator
{
    public static IReadOnlyList<BlockCountIssue> Evaluate(
        string editorAlias,
        string? valueJson,
        IReadOnlyList<BlockCountRule> rules,
        IReadOnlyDictionary<Guid, string>? blockNames = null)
    {
        JsonNode? value = null;
        if (!string.IsNullOrWhiteSpace(valueJson))
        {
            try
            {
                value = JsonNode.Parse(valueJson);
            }
            catch (JsonException)
            {
                value = null;
            }
        }

        return Evaluate(editorAlias, value, rules, blockNames);
    }

    public static IReadOnlyList<BlockCountIssue> Evaluate(
        string editorAlias,
        JsonNode? value,
        IReadOnlyList<BlockCountRule> rules,
        IReadOnlyDictionary<Guid, string>? blockNames = null)
    {
        var issues = new List<BlockCountIssue>();
        var counts = CountLayoutItems(editorAlias, value);

        foreach (var rule in rules.GroupBy(rule => rule.ContentElementTypeKey).Select(group => group.First()))
        {
            var name = blockNames is not null && blockNames.TryGetValue(rule.ContentElementTypeKey, out var resolved) && !string.IsNullOrWhiteSpace(resolved)
                ? resolved
                : "Block";

            if (rule.Min is < 0 || rule.Max is < 0)
            {
                issues.Add(Issue(rule, name, BlockCountIssueKind.InvalidConfiguration, BlockCountMessages.Negative(name)));
                continue;
            }

            if (rule.Min is int minimumBound && rule.Max is int maximumBound && minimumBound > maximumBound)
            {
                issues.Add(Issue(rule, name, BlockCountIssueKind.InvalidConfiguration, BlockCountMessages.MinimumAboveMaximum(name)));
                continue;
            }

            if (rule.Min is null && rule.Max is null)
            {
                continue;
            }

            counts.TryGetValue(rule.ContentElementTypeKey, out var count);

            if (rule.Min is int exact && rule.Max == exact && count != exact)
            {
                issues.Add(Issue(rule, name, BlockCountIssueKind.Exact, BlockCountMessages.Exact(name, exact, count)));
                continue;
            }

            if (rule.Min is int minimum && count < minimum)
            {
                issues.Add(Issue(rule, name, BlockCountIssueKind.BelowMinimum, BlockCountMessages.BelowMinimum(name, minimum, count)));
            }

            if (rule.Max is int maximum && count > maximum)
            {
                issues.Add(Issue(rule, name, BlockCountIssueKind.AboveMaximum, BlockCountMessages.AboveMaximum(name, maximum, count)));
            }
        }

        return issues;
    }

    internal static Dictionary<Guid, int> CountLayoutItems(string editorAlias, JsonNode? value)
    {
        var counts = new Dictionary<Guid, int>();
        if (value is not JsonObject root)
        {
            return counts;
        }

        var contentTypes = MapContentTypes(BlockCountJson.Property(root, "contentData") as JsonArray);
        if (BlockCountJson.Property(root, "layout") is not JsonObject layout)
        {
            return counts;
        }

        var items = FindLayout(layout, editorAlias);
        if (items is null)
        {
            return counts;
        }

        Walk(items, contentTypes, counts, BlockEditors.IsGrid(editorAlias));
        return counts;
    }

    internal static IEnumerable<NestedBlockValue> ReadNestedBlocks(JsonNode? value)
    {
        if (value is not JsonObject root)
        {
            yield break;
        }

        foreach (var collectionName in new[] { "contentData", "settingsData" })
        {
            if (BlockCountJson.Property(root, collectionName) is not JsonArray entries)
            {
                continue;
            }

            foreach (var entry in entries)
            {
                if (entry is not JsonObject block)
                {
                    continue;
                }

                if (!BlockCountJson.TryGuid(BlockCountJson.Property(block, "contentTypeKey"), out var elementTypeKey))
                {
                    continue;
                }

                if (BlockCountJson.Property(block, "values") is not JsonArray values)
                {
                    continue;
                }

                var blockKey = BlockCountJson.ReadString(BlockCountJson.Property(block, "key"));

                foreach (var rawProperty in values)
                {
                    if (rawProperty is not JsonObject property)
                    {
                        continue;
                    }

                    var alias = BlockCountJson.ReadString(BlockCountJson.Property(property, "alias"));
                    var editorAlias = BlockCountJson.ReadString(BlockCountJson.Property(property, "editorAlias"));
                    if (string.IsNullOrWhiteSpace(alias) || string.IsNullOrWhiteSpace(editorAlias) || !BlockEditors.IsSupported(editorAlias))
                    {
                        continue;
                    }

                    yield return new NestedBlockValue(
                        elementTypeKey,
                        blockKey,
                        alias,
                        editorAlias,
                        BlockCountJson.ReadString(BlockCountJson.Property(property, "culture")),
                        BlockCountJson.NormalizeBlockValue(BlockCountJson.Property(property, "value")));
                }
            }
        }
    }

    private static JsonArray? FindLayout(JsonObject layout, string editorAlias) =>
        BlockCountJson.Property(layout, editorAlias) as JsonArray;

    private static Dictionary<Guid, Guid> MapContentTypes(JsonArray? contentData)
    {
        var map = new Dictionary<Guid, Guid>();
        if (contentData is null)
        {
            return map;
        }

        foreach (var entry in contentData)
        {
            if (entry is not JsonObject block)
            {
                continue;
            }

            if (BlockCountJson.TryGuid(BlockCountJson.Property(block, "key"), out var key)
                && BlockCountJson.TryGuid(BlockCountJson.Property(block, "contentTypeKey"), out var contentTypeKey))
            {
                map[key] = contentTypeKey;
            }
        }

        return map;
    }

    private static void Walk(JsonArray items, Dictionary<Guid, Guid> contentTypes, Dictionary<Guid, int> counts, bool includeAreas)
    {
        foreach (var item in items)
        {
            if (item is not JsonObject layoutItem)
            {
                continue;
            }

            if (BlockCountJson.TryGuid(BlockCountJson.Property(layoutItem, "contentKey"), out var contentKey)
                && contentTypes.TryGetValue(contentKey, out var contentTypeKey))
            {
                counts[contentTypeKey] = counts.GetValueOrDefault(contentTypeKey) + 1;
            }

            if (!includeAreas || BlockCountJson.Property(layoutItem, "areas") is not JsonArray areas)
            {
                continue;
            }

            foreach (var area in areas)
            {
                if (area is JsonObject areaObject && BlockCountJson.Property(areaObject, "items") is JsonArray areaItems)
                {
                    Walk(areaItems, contentTypes, counts, includeAreas);
                }
            }
        }
    }

    private static BlockCountIssue Issue(BlockCountRule rule, string name, BlockCountIssueKind kind, string message) =>
        new()
        {
            ContentElementTypeKey = rule.ContentElementTypeKey,
            BlockName = name,
            Kind = kind,
            Message = message,
        };
}

internal sealed record NestedBlockValue(
    Guid ElementTypeKey,
    string? BlockKey,
    string Alias,
    string EditorAlias,
    string? Culture,
    JsonNode? Value);
