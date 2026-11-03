using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;

namespace BlockCount;

public sealed class BlockCountListConfigurationEditor : ConfigurationEditor<BlockCountListConfiguration>
{
    public BlockCountListConfigurationEditor(IIOHelper ioHelper, IEditorConfigurationParser editorConfigurationParser)
        : base(ioHelper, editorConfigurationParser)
    {
    }

    public override BlockCountListConfiguration? FromConfigurationEditor(
        IDictionary<string, object?>? editorValues,
        BlockCountListConfiguration? configuration)
    {
        var parsed = base.FromConfigurationEditor(editorValues, configuration) ?? new BlockCountListConfiguration();
        if (editorValues is not null && editorValues.Keys.Any(key => string.Equals(key, "blocks", StringComparison.OrdinalIgnoreCase)))
        {
            parsed.BlockCounts = BlockCountEditorValues.Read(editorValues);
        }

        return parsed;
    }

    public override Dictionary<string, object> ToConfigurationEditor(BlockCountListConfiguration? configuration)
    {
        var editorValues = base.ToConfigurationEditor(configuration);
        BlockCountEditorValues.Apply(editorValues, configuration?.BlockCounts);
        return editorValues;
    }
}
