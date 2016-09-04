using Newtonsoft.Json;

namespace ACMESharp.Providers.CloudFlare.Results
{
    internal class ResultInfo
    {
        public int Page { get; set; }
        [JsonProperty("per_page")]
        public int PerPage { get; set; }
        [JsonProperty("total_pages")]
        public int TotalPages { get; set; }
        public int Count { get; set; }
        [JsonProperty("total_count")]
        public int TotalCount { get; set; }
    }
}