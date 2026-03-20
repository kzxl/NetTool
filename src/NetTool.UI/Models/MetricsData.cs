using System.Text.Json.Serialization;

namespace NetTool.UI.Models
{
    public class MetricsData
    {
        [JsonPropertyName("t")]
        public int Time { get; set; }

        [JsonPropertyName("rps")]
        public int Rps { get; set; }

        [JsonPropertyName("avg")]
        public double Avg { get; set; }

        [JsonPropertyName("p50")]
        public double P50 { get; set; }

        [JsonPropertyName("p95")]
        public double P95 { get; set; }

        [JsonPropertyName("p99")]
        public double P99 { get; set; }

        [JsonPropertyName("err")]
        public double ErrorRate { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("success")]
        public int Success { get; set; }

        [JsonPropertyName("fail")]
        public int Fail { get; set; }
    }
}
