using Umbraco.Cms.Core;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;

namespace BlockCount;

[DataEditor(
    Constants.PropertyEditors.Aliases.BlockGrid,
    "Block Grid",
    "blockgrid",
    ValueType = ValueTypes.Json,
    Group = Constants.PropertyEditors.Groups.RichContent,
    Icon = "icon-layout")]
public sealed class BlockCountGridPropertyEditor : BlockGridPropertyEditor
{
    private readonly IIOHelper _ioHelper;
    private readonly IEditorConfigurationParser _parser;

    public BlockCountGridPropertyEditor(
        IDataValueEditorFactory dataValueEditorFactory,
        IIOHelper ioHelper,
        IBlockValuePropertyIndexValueFactory blockValuePropertyIndexValueFactory,
        IEditorConfigurationParser editorConfigurationParser)
        : base(dataValueEditorFactory, ioHelper, blockValuePropertyIndexValueFactory)
    {
        _ioHelper = ioHelper;
        _parser = editorConfigurationParser;
    }

    protected override IConfigurationEditor CreateConfigurationEditor() =>
        new BlockCountGridConfigurationEditor(_ioHelper, _parser);
}
