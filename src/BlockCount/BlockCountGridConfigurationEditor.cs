using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;

namespace BlockCount;

public sealed class BlockCountGridConfigurationEditor : ConfigurationEditor<BlockCountGridConfiguration>
{
    public BlockCountGridConfigurationEditor(IIOHelper ioHelper, IEditorConfigurationParser editorConfigurationParser)
        : base(ioHelper, editorConfigurationParser)
    {
    }

    public override BlockCountGridConfiguration? FromConfigurationEditor(
        IDictionary<string, object?>? editorValues,
        BlockCountGridConfiguration? configuration)
    {
        var parsed = base.FromConfigurationEditor(editorValues, configuration) ?? new BlockCountGridConfiguration();
        if (editorValues is not null && editorValues.Keys.Any(key => string.Equals(key, "blocks", StringComparison.OrdinalIgnoreCase)))
        {
            parsed.BlockCounts = BlockCountEditorValues.Read(editorValues);
        }

        return parsed;
    }

    public override Dictionary<string, object> ToConfigurationEditor(BlockCountGridConfiguration? configuration)
    {
        var editorValues = base.ToConfigurationEditor(configuration);
        BlockCountEditorValues.Apply(editorValues, configuration?.BlockCounts);
        return editorValues;
    }
}
