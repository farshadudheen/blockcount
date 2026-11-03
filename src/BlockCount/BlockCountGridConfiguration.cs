using Newtonsoft.Json;
using Umbraco.Cms.Core.PropertyEditors;

namespace BlockCount;

public sealed class BlockCountGridConfiguration : BlockGridConfiguration
{
    [JsonProperty("blockCounts")]
    public BlockCountLimit[]? BlockCounts { get; set; }
}
