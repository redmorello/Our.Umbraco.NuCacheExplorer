using System;
using Newtonsoft.Json;

namespace Our.Umbraco.NuCacheExplorer.Models
{
    public class CultureVariation
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("urlSegment")]
        public string UrlSegment { get; set; }

        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [JsonProperty("isDraft")]
        public bool IsDraft { get; set; }
    }
}
