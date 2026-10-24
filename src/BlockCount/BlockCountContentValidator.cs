using System.Text.Json.Nodes;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace BlockCount;

public sealed class BlockCountContentValidator
{
    private readonly IDataTypeService _dataTypeService;
    private readonly IContentTypeService _contentTypeService;

    public BlockCountContentValidator(IDataTypeService dataTypeService, IContentTypeService contentTypeService)
    {
        _dataTypeService = dataTypeService;
        _contentTypeService = contentTypeService;
    }

    public async Task<IReadOnlyList<string>> ValidateAsync(
        IContent content,
        Func<string, bool> isCulturePublishing,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<string>();
        var names = new Dictionary<Guid, string>();

        foreach (var property in content.Properties)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!BlockEditors.IsSupported(property.PropertyType.PropertyEditorAlias))
            {
                continue;
            }

            if (property.PropertyType.VariesByCulture())
            {
                foreach (var culture in content.AvailableCultures)
                {
                    if (!isCulturePublishing(culture))
                    {
                        continue;
                    }

                    await ValidatePropertyAsync(
                        property.PropertyType,
                        property.GetValue(culture),
                        Location(property.PropertyType.Name, culture),
                        culture,
                        isCulturePublishing,
                        names,
                        messages,
                        cancellationToken,
                        depth: 0).ConfigureAwait(false);
                }
            }
            else
            {
                await ValidatePropertyAsync(
                    property.PropertyType,
                    property.GetValue(),
                    property.PropertyType.Name,
                    culture: null,
                    isCulturePublishing,
                    names,
                    messages,
                    cancellationToken,
                    depth: 0).ConfigureAwait(false);
            }
        }

        return messages;
    }

    private async Task ValidatePropertyAsync(
        IPropertyType propertyType,
        object? rawValue,
        string location,
        string? culture,
        Func<string, bool> isCulturePublishing,
        Dictionary<Guid, string> names,
        List<string> messages,
        CancellationToken cancellationToken,
        int depth)
    {
        var value = ParseStoredValue(rawValue);
        var rules = await ReadRulesAsync(propertyType, cancellationToken).ConfigureAwait(false);
        if (rules.Count > 0)
        {
            var ruleNames = await ResolveNamesAsync(rules.Select(rule => rule.ContentElementTypeKey), names, cancellationToken).ConfigureAwait(false);
            foreach (var issue in BlockCountEvaluator.Evaluate(propertyType.PropertyEditorAlias, value, rules, ruleNames))
            {
                messages.Add($"{location}: {issue.Message}");
            }
        }

        await ValidateNestedAsync(value, location, culture, isCulturePublishing, names, messages, cancellationToken, depth).ConfigureAwait(false);
    }

    private async Task ValidateNestedAsync(
        JsonNode? value,
        string location,
        string? culture,
        Func<string, bool> isCulturePublishing,
        Dictionary<Guid, string> names,
        List<string> messages,
        CancellationToken cancellationToken,
        int depth)
    {
        if (value is null || depth >= 32)
        {
            return;
        }

        var seenInvariant = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var nested in BlockCountEvaluator.ReadNestedBlocks(value))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var elementType = await _contentTypeService.GetAsync(nested.ElementTypeKey).ConfigureAwait(false);
            var propertyType = elementType?.CompositionPropertyTypes.FirstOrDefault(property =>
                string.Equals(property.Alias, nested.Alias, StringComparison.OrdinalIgnoreCase));
            if (propertyType is null || !BlockEditors.IsSupported(propertyType.PropertyEditorAlias))
            {
                continue;
            }

            if (!IncludeNested(nested.Culture, culture, propertyType.VariesByCulture(), isCulturePublishing))
            {
                continue;
            }

            if (!propertyType.VariesByCulture())
            {
                var token = $"{nested.BlockKey}:{propertyType.Alias}";
                if (!seenInvariant.Add(token))
                {
                    continue;
                }
            }

            var elementName = await ResolveNameAsync(nested.ElementTypeKey, names, cancellationToken).ConfigureAwait(false);
            var nestedLocation = $"{location} / {elementName} / {propertyType.Name}";
            var nestedCulture = propertyType.VariesByCulture() ? nested.Culture ?? culture : null;
            await ValidatePropertyAsync(
                propertyType,
                nested.Value,
                nestedLocation,
                nestedCulture,
                isCulturePublishing,
                names,
                messages,
                cancellationToken,
                depth + 1).ConfigureAwait(false);
        }
    }

    internal static bool IncludeNested(string? valueCulture, string? parentCulture, bool nestedVaries, Func<string, bool> isCulturePublishing)
    {
        if (!nestedVaries)
        {
            return string.IsNullOrEmpty(valueCulture);
        }

        if (string.IsNullOrEmpty(valueCulture))
        {
            return true;
        }

        if (parentCulture is not null
            && !string.Equals(valueCulture, parentCulture, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return isCulturePublishing(valueCulture);
    }

    private async Task<IReadOnlyList<BlockCountRule>> ReadRulesAsync(IPropertyType propertyType, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dataType = await _dataTypeService.GetAsync(propertyType.DataTypeKey).ConfigureAwait(false);
        return dataType is null ? [] : BlockCountConfigurationReader.Read(dataType.ConfigurationData);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> ResolveNamesAsync(
        IEnumerable<Guid> keys,
        Dictionary<Guid, string> names,
        CancellationToken cancellationToken)
    {
        foreach (var key in keys.Distinct())
        {
            await ResolveNameAsync(key, names, cancellationToken).ConfigureAwait(false);
        }

        return names;
    }

    private async Task<string> ResolveNameAsync(Guid key, Dictionary<Guid, string> names, CancellationToken cancellationToken)
    {
        if (names.TryGetValue(key, out var cached))
        {
            return cached;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var contentType = await _contentTypeService.GetAsync(key).ConfigureAwait(false);
        var name = string.IsNullOrWhiteSpace(contentType?.Name) ? "Block" : contentType.Name;
        names[key] = name;
        return name;
    }

    private static JsonNode? ParseStoredValue(object? rawValue)
    {
        if (rawValue is JsonNode node)
        {
            return node;
        }

        return BlockCountJson.ToNode(rawValue);
    }

    private static string Location(string propertyName, string culture) => $"{propertyName} ({culture})";
}
