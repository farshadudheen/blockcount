using Newtonsoft.Json;
using Umbraco.Cms.Core.PropertyEditors;

namespace BlockCount;

public sealed class BlockCountListConfiguration : BlockListConfiguration
{
    [JsonProperty("blockCounts")]
    public BlockCountLimit[]? BlockCounts { get; set; }
}
