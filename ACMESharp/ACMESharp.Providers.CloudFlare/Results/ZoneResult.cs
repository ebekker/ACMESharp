using Newtonsoft.Json;

namespace ACMESharp.Providers.CloudFlare.Results
{
    internal class ZoneResult
    {
        public Zone[] Result { get; set; }
        [JsonProperty("result_info")]
        public ResultInfo ResultInfo { get; set; }
    }
}