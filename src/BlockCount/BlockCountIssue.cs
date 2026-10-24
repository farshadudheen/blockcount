namespace BlockCount;

public enum BlockCountIssueKind
{
    BelowMinimum,
    AboveMaximum,
    Exact,
    InvalidConfiguration,
}

public sealed class BlockCountIssue
{
    public Guid ContentElementTypeKey { get; init; }

    public string BlockName { get; init; } = "Block";

    public string Message { get; init; } = string.Empty;

    public BlockCountIssueKind Kind { get; init; }
}
