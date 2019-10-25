using Newtonsoft.Json;

namespace Our.Umbraco.NuCacheExplorer.Models
{
    public class PropertyData
    {
        [JsonProperty("culture")]
        public string Culture { get; set; }

        [JsonProperty("seg")]
        public string Segment { get; set; }

        [JsonProperty("val")]
        public object Value { get; set; }
    }
}
