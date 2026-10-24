namespace BlockCount;

public sealed class BlockCountRule
{
    public Guid ContentElementTypeKey { get; init; }

    public int? Min { get; init; }

    public int? Max { get; init; }
}
