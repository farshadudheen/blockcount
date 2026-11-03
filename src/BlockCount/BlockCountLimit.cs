using Newtonsoft.Json;

namespace BlockCount;

public sealed class BlockCountLimit
{
    [JsonProperty("contentElementTypeKey")]
    public Guid ContentElementTypeKey { get; set; }

    [JsonProperty("min")]
    public int? Min { get; set; }

    [JsonProperty("max")]
    public int? Max { get; set; }
}
