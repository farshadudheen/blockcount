using Umbraco.Cms.Core;

namespace BlockCount;

public static class BlockEditors
{
    public const string BlockList = Constants.PropertyEditors.Aliases.BlockList;

    public const string BlockGrid = Constants.PropertyEditors.Aliases.BlockGrid;

    public static bool IsSupported(string? editorAlias) =>
        string.Equals(editorAlias, BlockList, StringComparison.OrdinalIgnoreCase)
        || string.Equals(editorAlias, BlockGrid, StringComparison.OrdinalIgnoreCase);

    public static bool IsGrid(string? editorAlias) =>
        string.Equals(editorAlias, BlockGrid, StringComparison.OrdinalIgnoreCase);
}
