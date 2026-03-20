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

        [JsonPropertyName("min")]
        public double Min { get; set; }

        [JsonPropertyName("max")]
        public double Max { get; set; }

        [JsonPropertyName("stddev")]
        public double StdDev { get; set; }

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

        [JsonPropertyName("codes")]
        public StatusCodeDist? StatusCodes { get; set; }

        [JsonPropertyName("bytes")]
        public long BytesReceived { get; set; }

        [JsonPropertyName("conns")]
        public long ActiveConnections { get; set; }
    }

    public class StatusCodeDist
    {
        [JsonPropertyName("2xx")]
        public int S2xx { get; set; }

        [JsonPropertyName("3xx")]
        public int S3xx { get; set; }

        [JsonPropertyName("4xx")]
        public int S4xx { get; set; }

        [JsonPropertyName("5xx")]
        public int S5xx { get; set; }
    }
}
