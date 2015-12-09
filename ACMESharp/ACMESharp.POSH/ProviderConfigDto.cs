using Newtonsoft.Json;

namespace ACMESharp.POSH
{
    public class ProviderConfigDto
    {
        public Provider Provider { get; set; }
    }

    public class Provider
    {
        [JsonProperty("$type")]
        public string Type { get; set; }

        [JsonProperty("FilePath")]
        public string FilePath { get; set; }
    }
}
