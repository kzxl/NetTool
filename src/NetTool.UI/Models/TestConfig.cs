using System.Text.Json.Serialization;

namespace NetTool.UI.Models
{
    public class TestConfig
    {
        [JsonPropertyName("request")]
        public RequestConfig Request { get; set; } = new();

        [JsonPropertyName("load")]
        public LoadConfig Load { get; set; } = new();

        [JsonPropertyName("timeoutMs")]
        public int TimeoutMs { get; set; } = 5000;
    }

    public class RequestConfig
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = "";

        [JsonPropertyName("method")]
        public string Method { get; set; } = "GET";

        [JsonPropertyName("headers")]
        public Dictionary<string, string> Headers { get; set; } = new();

        [JsonPropertyName("body")]
        public string Body { get; set; } = "";

        [JsonPropertyName("expectedStatus")]
        public int ExpectedStatus { get; set; }
    }

    public class LoadConfig
    {
        [JsonPropertyName("concurrency")]
        public int Concurrency { get; set; } = 10;

        [JsonPropertyName("durationSec")]
        public int DurationSec { get; set; } = 30;

        [JsonPropertyName("rampUpSec")]
        public int RampUpSec { get; set; } = 5;
    }
}
