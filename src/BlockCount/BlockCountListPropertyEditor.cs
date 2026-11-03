using Umbraco.Cms.Core;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;

namespace BlockCount;

[DataEditor(
    Constants.PropertyEditors.Aliases.BlockList,
    "Block List",
    "blocklist",
    ValueType = ValueTypes.Json,
    Group = Constants.PropertyEditors.Groups.Lists,
    Icon = "icon-thumbnail-list",
    ValueEditorIsReusable = false)]
public sealed class BlockCountListPropertyEditor : BlockListPropertyEditor
{
    private readonly IIOHelper _ioHelper;
    private readonly IEditorConfigurationParser _parser;

    public BlockCountListPropertyEditor(
        IDataValueEditorFactory dataValueEditorFactory,
        PropertyEditorCollection propertyEditors,
        IIOHelper ioHelper,
        IEditorConfigurationParser editorConfigurationParser,
        IBlockValuePropertyIndexValueFactory blockValuePropertyIndexValueFactory)
        : base(dataValueEditorFactory, propertyEditors, ioHelper, editorConfigurationParser, blockValuePropertyIndexValueFactory)
    {
        _ioHelper = ioHelper;
        _parser = editorConfigurationParser;
    }

    protected override IConfigurationEditor CreateConfigurationEditor() =>
        new BlockCountListConfigurationEditor(_ioHelper, _parser);
}
